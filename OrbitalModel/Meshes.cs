﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrbitalModel.Graphics;
using OpenTK.Mathematics;

namespace OrbitalModel;

public static class Meshes
{
    public static MeshBuilder CreateOrigin()
    {
        var red = Color4.DarkRed;
        var green = Color4.ForestGreen;
        var blue = Color4.DeepSkyBlue;
        return new MeshBuilder()
            .SetVertexColor(blue)
            .AddVertex(0, 0, 0, "xy0")
            .AddVertex(1, 0, 0, "xy1")
            .AddVertex(0, 1, 0, "xy2")
            .AddTri("xy0", "xy1", "xy2")
            .SetVertexColor(green)
            .AddVertex(0, 0, 0, "xz0")
            .AddVertex(1, 0, 0, "xz1")
            .AddVertex(0, 0, 1, "xz2")
            .AddTri("xz0", "xz1", "xz2")
            .SetVertexColor(red)
            .AddVertex(0, 0, 0, "yz0")
            .AddVertex(0, 1, 0, "yz1")
            .AddVertex(0, 0, 1, "yz2")
            .AddTri("yz0", "yz1", "yz2")
            .Translate(-0.001f, -0.001f, -0.001f)
            .Scale(0.5f)
            
            .JoinWith(new MeshBuilder().SetVertexColor(red).CreateCube().Scale(2, 0.05f, 0.05f))
            .JoinWith(new MeshBuilder().SetVertexColor(green).CreateCube().Scale(0.05f, 2, 0.05f))
            .JoinWith(new MeshBuilder().SetVertexColor(blue).CreateCube().Scale(0.05f, 0.05f, 2));
    }

    public static MeshBuilder CreateBodyMarker(this MeshBuilder meshBuilder)
    {
        return meshBuilder
            .AddVertex(0, 0, 0, "o")
            .AddVertex(1, 0, 1, "x")
            .AddVertex(-1, 0, 1, "-x")
            .AddVertex(0, 1, 1, "y")
            .AddVertex(0, -1, 1, "-y")
            .AddTri("o", "x", "y")
            .AddTri("o", "x", "-y")
            .AddTri("o", "-x", "y")
            .AddTri("o", "-x", "-y")
            .AddQuad("x", "y", "-x", "-y");
    }

    public static MeshBuilder CreateArrow(this MeshBuilder meshBuilder)
    {
        var guid = Guid.NewGuid();
        return meshBuilder
            .SetKeyTransformer(id => $"{guid}-{id}")
            .AddVertex(0, 0, 0, "o")
            .AddVertex(1, 0, -1, "x")
            .AddVertex(-1, 0, -1, "-x")
            .AddVertex(0, 1, -1, "y")
            .AddVertex(0, -1, -1, "-y")
            .AddTri("o", "x", "y")
            .AddTri("o", "x", "-y")
            .AddTri("o", "-x", "y")
            .AddTri("o", "-x", "-y")
            .AddQuad("x", "y", "-x", "-y")
            .Translate(0, 0, 1)
            .ResetKeyTransformer();
    }

    public static MeshBuilder CreateCom(this MeshBuilder meshBuilder)
    {
        var width = 0.02f;
        return meshBuilder
            .JoinWith(new MeshBuilder()
                .SetVertexColor(Color4.White)
                .CreateCenteredCube()
                .Scale(width, width, 1.0f)) // z axis
            .JoinWith(new MeshBuilder()
                .SetVertexColor(Color4.White)
                .CreateCenteredCube()
                .Scale(width, 1.0f, width)) // y axis
            .JoinWith(new MeshBuilder()
                .SetVertexColor(Color4.White)
                .CreateCenteredCube()
                .Scale(1.0f, width, width));// x axis
            
    }

    public static MeshBuilder CreateCube(this MeshBuilder meshBuilder)
    {
        return meshBuilder
            .SetRandomKeyTransformer("cube")

            .AddVertex(0, 0, 0, "o")
            .AddVertex(1, 0, 0, "x")
            .AddVertex(1, 1, 0, "xy")
            .AddVertex(0, 1, 0, "y")

            .AddVertex(0, 0, 1, "z")
            .AddVertex(1, 0, 1, "zx")
            .AddVertex(1, 1, 1, "zxy")
            .AddVertex(0, 1, 1, "zy")

            .AddQuad("o", "x", "xy", "y")
            .AddQuad("o", "x", "zx", "z") 
            .AddQuad("z", "zx", "zxy", "zy")
            .AddQuad("y", "xy", "zxy", "zy")
            .AddQuad("o", "y", "zy", "z")
            .AddQuad("x", "xy", "zxy", "zx")

            .ResetKeyTransformer();
    }

    public static MeshBuilder CreateCenteredCube(this MeshBuilder meshBuilder) =>
        meshBuilder.CreateCube().Translate(-0.5f, -0.5f, -0.5f);
}

