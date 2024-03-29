﻿using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL;
using ImGuiNET;
using CommandLine;
using System.Text.Json.Serialization;

using OrbitalModel.Graphics;
using System.Text.Json;

namespace OrbitalModel;

#nullable disable

public class Options
{
    [Value(0, Required = true, HelpText = "File that describes the initial state of the simulation.")]
    public string InputFilePath { get; set; }
}

#nullable enable

public class Program
{
    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(options =>
            {
                var inputFile = File.ReadAllText(options.InputFilePath);
                var initialStateData = JsonSerializer.Deserialize<InitialStateData>(inputFile, new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip });
                if (initialStateData is null) throw new Exception("Json object is null.");
                Run(initialStateData);
            })
            .WithNotParsed(errors =>
            {

            });
    }

    public static void Run(InitialStateData initialStateData)
    {
        var width = 1200;
        var height = 800;
        var updateFrequency = 60.0f;

        var gws = GameWindowSettings.Default;
        var nws = NativeWindowSettings.Default;
        gws.RenderFrequency = 60.0;
        gws.UpdateFrequency = updateFrequency;

        nws.IsEventDriven = false;
        nws.API = ContextAPI.OpenGL;
        nws.APIVersion = Version.Parse("4.1");
        nws.AutoLoadBindings = true;
        nws.WindowState = WindowState.Normal;
        nws.Size = new(width, height);
        nws.Title = "Orbital Model";
        
        var window = new GameWindow(gws, nws);

        ImGuiController gui = null!;

        var scale = (float)initialStateData.VectorFieldSize;

        var vp = new SimulationViewport()
        {
            Width = width,
            Height = height,
            SimulationFrequency = updateFrequency,
            G = 6.6743e-11f,
            Scale = (float)initialStateData.Scale,
            DtSignificand = (float)initialStateData.TimeStep,
            DtExponent = 0,
            StepsPerFrame = 100,
            VectorFieldXMin = -scale,
            VectorFieldXMax = scale,
            VectorFieldYMin = -scale,
            VectorFieldYMax = scale,
            VectorFieldZMin = -scale,
            VectorFieldZMax = scale,
            VectorFieldSpacing = scale / 5,
        };  

        window.Load += () =>
        {
            gui = new ImGuiController(width, height);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.CullFace);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            vp.Init();
            var bodies = new List<Body>();
            foreach (var bodyData in initialStateData.Bodies)
            {
                var body = new Body(bodyData.Mass, bodyData.Position_Vector, bodyData.Velocity_Vector, vp.Shader, bodyData.Name, bodyData.Color_Color4);
                body.ShowTrailRef = bodyData.ShowTrail;
                bodies.Add(body);
            }
            foreach (var orbitalBodyData in initialStateData.OrbitalBodies)
            {
                var orbitalBody = new OrbitalBody(
                    eccentricity: orbitalBodyData.E,
                    semimajorAxis: orbitalBodyData.A,
                    inclination: orbitalBodyData.I * 0.0174533,
                    longitudeOfAscendingNode: orbitalBodyData.O * 0.0174533,
                    argumentOfPeriapsis: orbitalBodyData.W * 0.0174533,
                    pericenterEpoch: orbitalBodyData.Tp,
                    period: orbitalBodyData.T);
                var referenceBody = bodies.Find(body => body.Name == orbitalBodyData.Reference);
                if (referenceBody is null) throw new Exception($"Referenced body `{orbitalBodyData.Reference}` does not exist. Referenced bodies need to be defined earlier than dependents.");
                (var position, var velocity) = orbitalBody.InitialConditions(initialStateData.InitialTime);

                var body = new Body(orbitalBodyData.Mass, position + referenceBody.Position, velocity + referenceBody.Velocity, vp.Shader, orbitalBodyData.Name, orbitalBodyData.Color_Color4);
                body.ShowTrailRef = orbitalBodyData.ShowTrail;
                bodies.Add(body);
            }
            foreach (var body in bodies)
            {
                vp.AddBody(body);
            }
        };
        vp.AddToWindow(window);
        window.RenderFrame += args =>
        {
            GL.Enable(EnableCap.DepthTest);

            GL.ClearColor(0.07f, 0.13f, 0.17f, 1.0f);
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            // Render viewport

            vp.Render();

            // Render gui

            gui.Update(window, (float)args.Time);
            // ImGui.DockSpaceOverViewport();
            // if (ImGui.Begin("viewport"))
            // {
            // 
            // }

            if (ImGui.Begin("options"))
            {
                // Camera
                ImGui.Text("camera");
                ImGui.Separator();
                if (ImGui.Button("reset camera"))
                {
                    vp.ResetCamera();
                }
                if (ImGui.Button("look at origin"))
                {
                    vp.LookAtOrigin();
                }
                ImGui.SliderFloat("field of view (degrees)", ref vp.FovDegrees, 10.0f, 179.0f);
                vp.ApplyFov();
                
                ImGui.LabelText("position", vp.CameraPositionString);
                ImGui.LabelText("target", vp.CameraTargetString);
                ImGui.LabelText("distance to target", $"{vp.DistanceToTargetDisplay, 3}");

                // Simulation
                ImGui.Text("simulation");
                ImGui.Separator();

                if (vp.Paused)
                {
                    if (ImGui.Button("play"))
                    {
                        vp.Paused = false;
                    }
                }
                else
                {
                    if (ImGui.Button("pause"))
                    {
                        vp.Paused = true;
                    }
                }
                ImGui.SameLine();
                ImGui.Text(vp.Paused ? "Paused" : "Running");

                // ImGui.SliderFloat("time step significand", ref vp.DtSignificand, 1f, 10f);
                // ImGui.SliderInt("time step exponent", ref vp.DtExponent, -6, 6);
                ImGui.DragFloat("time step", ref vp.DtSignificand, 0.5f);
                ImGui.SliderInt("steps per frame", ref vp.StepsPerFrame, 1, 10000);
                ImGui.LabelText("time step", $"{vp.RealDtDisplay} s");
                ImGui.LabelText("simulation framerate", $"{vp.SimulationFrequency} fps");
                ImGui.LabelText("time scale", $"{vp.TimeScaleDisplay} s (sim) = 1 s (real)");
                // Implement this!
                ImGui.DragFloat("rendering scale factor", ref vp.Scale, 0.001f);
                // Vector field
                ImGui.Text("vector field");
                ImGui.Separator();
                ImGui.Checkbox("show acceleration vector field", ref vp.ShowAccelerationField);
                ImGui.SameLine();
                if (ImGui.Button("regenerate"))
                {
                    vp.RegenerateAccelerationField();
                }

                ImGui.PushItemWidth(100f);
                ImGui.DragFloat("x min", ref vp.VectorFieldXMin, v_speed: 0.5f);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.DragFloat("y min", ref vp.VectorFieldYMin, v_speed: 0.5f);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.DragFloat("z min", ref vp.VectorFieldZMin, v_speed: 0.5f);
                ImGui.PopItemWidth();

                ImGui.PushItemWidth(100f);
                ImGui.DragFloat("x max", ref vp.VectorFieldXMax, v_speed: 0.5f);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.DragFloat("y max", ref vp.VectorFieldYMax, v_speed: 0.5f);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGui.DragFloat("z max", ref vp.VectorFieldZMax, v_speed: 0.5f);
                ImGui.PopItemWidth();

                ImGui.DragFloat("spacing", ref vp.VectorFieldSpacing, 0.1f, 10f);

                ImGui.LabelText("number of vectors", $"{vp.AccelerationFieldNumVectors}");

                // Center of mass
                ImGui.Text("center of mass");
                ImGui.Separator();
                ImGui.Checkbox("show center of mass", ref vp.ShowCenterOfMass);

                ImGui.End();
            }

            if (ImGui.Begin("bodies"))
            {
                foreach (var body in vp.Bodies)
                {
                    ImGui.PushID(body.Name);
                    if (ImGui.CollapsingHeader($"{body.Name}"))
                    {
                        ImGui.ColorButton($"{body.Name}", new System.Numerics.Vector4(body.Color.R, body.Color.G, body.Color.B, 1));
                        ImGui.LabelText("position", $"({body.Position.X:0.000e0}, {body.Position.Y:0.000e0}, {body.Position.Z:0.000e0})");
                        ImGui.Checkbox($"show velocity vector", ref body.ShowVeloctiyRef);
                        ImGui.Checkbox($"show trail", ref body.ShowTrailRef);
                        if (ImGui.Button("clear trail"))
                        {
                            body.ClearTrail();
                        }
                    }
                    ImGui.PopID();
                }
            }

            // if (ImGui.Begin("debug"))
            // {
            //     ImGui.LabelText("render framerate", $"{vp.RenderFramerateDisplay} fps");
            //     ImGui.LabelText("render frame time", $"{vp.RenderFrameTimeDisplay} ms");
            //     ImGui.LabelText("update framerate", $"{vp.UpdateFramerateDisplay} fps");
            //     ImGui.LabelText("update frame time", $"{vp.UpdateFrameTimeDisplay} ms");
            // 
            //     ImGui.End();
            // }

            ImGui.ShowMetricsWindow();

            gui.Render();

            // End rendering

            window.SwapBuffers();
        };
        window.Resize += args =>
        {
            GL.Viewport(0, 0, args.Width, args.Height);
            gui.WindowResized(args.Width, args.Height);
            vp.Resize(args.Width, args.Height);
        };
        window.TextInput += args =>
        {
            gui.PressChar((char)args.Unicode);
        };
        window.Run();
    }
}