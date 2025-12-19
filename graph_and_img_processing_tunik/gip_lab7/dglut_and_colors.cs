// DGLUT interop and shared color constants

using ObjLoader.Loader.Loaders;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Resources;
using System.Reflection;


// Color constants (arrays of 3 floats) used in multiple lab works
public static class CColors
{
    public static readonly float[] C_White     = { 1.0f, 1.0f, 1.0f };
    public static readonly float[] C_Black     = { 0.0f, 0.0f, 0.0f };
    public static readonly float[] C_Grey      = { 0.5f, 0.5f, 0.5f };
    public static readonly float[] C_DarcGrey  = { 0.2f, 0.2f, 0.2f };
    public static readonly float[] C_Red       = { 1.0f, 0.0f, 0.0f };
    public static readonly float[] C_Green     = { 0.0f, 1.0f, 0.0f };
    public static readonly float[] C_Blue      = { 0.0f, 0.0f, 1.0f };
    public static readonly float[] C_DarcBlue  = { 0.0f, 0.0f, 0.5f };
    public static readonly float[] C_Cyan      = { 0.0f, 1.0f, 1.0f };
    public static readonly float[] C_Magenta   = { 1.0f, 0.0f, 1.0f };

    public static readonly float[] C_Yellow    = { 1.0f, 1.0f, 0.0f };
    public static readonly float[] C_Orange    = { 0.1f, 0.5f, 0.0f };
    public static readonly float[] C_Lemon     = { 0.8f, 1.0f, 0.0f };
    public static readonly float[] C_Brown     = { 0.5f, 0.3f, 0.0f };
    public static readonly float[] C_Navy      = { 0.0f, 0.4f, 0.8f };
    public static readonly float[] C_Aqua      = { 0.4f, 0.7f, 1.0f };
    public static readonly float[] C_Cherry    = { 1.0f, 0.0f, 0.5f };

    // Helper to set GL color from an array
    public static void GlColorFromArray(float[] c)
    {
        if (c == null || c.Length < 3) return;
        GL.Color3(c[0], c[1], c[2]);
    }
}

public class TeapotRenderer
{
    private LoadResult teapotModel;

    // Load once at initialization - loads the embedded resource named "UtahTeapot" from the gip_lab7 resources.
    public void LoadTeapotFromResources(string resourceName = "UtahTeapot")
    {
        // Resource manager for the gip_lab7 Resources.resx in this project
        var rm = new ResourceManager("gip_lab7.Resources", Assembly.GetExecutingAssembly());
        object obj = null;
        try
        {
            obj = rm.GetObject(resourceName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get resource '{resourceName}': {ex.Message}");
            obj = null;
        }

        if (obj == null)
        {
            Debug.WriteLine($"Resource '{resourceName}' not found.");
            return;
        }

        Stream stream = null;

        if (obj is byte[] bytes)
        {
            stream = new MemoryStream(bytes);
        }
        else if (obj is string s)
        {
            // Resource could be stored as a file path
            if (File.Exists(s))
                stream = new FileStream(s, FileMode.Open, FileAccess.Read);
            else
            {
                Debug.WriteLine($"Resource '{resourceName}' is a string but file not found: {s}");
                return;
            }
        }
        else if (obj is Stream sstream)
        {
            stream = sstream;
        }
        else
        {
            Debug.WriteLine($"Unsupported resource type for '{resourceName}': {obj.GetType()}");
            return;
        }

        try
        {
            var objLoaderFactory = new ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create();

            using (stream)
            {
                teapotModel = objLoader.Load(stream);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load OBJ from resource '{resourceName}': {ex.Message}");
            teapotModel = null;
        }
    }

    // Call this in your render loop
    public void DrawTeapot(double size)
    {
        if (teapotModel == null) return;

        GL.PushMatrix();
        GL.Scale(size, size, size);

        // Iterate groups and faces; triangulate polygons using a triangle fan and compute normals when missing
        foreach (var group in teapotModel.Groups)
        {
            foreach (var face in group.Faces)
            {
                // triangulate any polygon face into triangles (0,i,i+1)
                if (face.Count < 3) continue;

                for (int t = 1; t < face.Count - 1; t++)
                {
                    // indices for triangle vertices in face
                    var fv0 = face[0];
                    var fv1 = face[t];
                    var fv2 = face[t + 1];

                    // Retrieve vertex positions
                    var v0 = teapotModel.Vertices[fv0.VertexIndex - 1];
                    var v1 = teapotModel.Vertices[fv1.VertexIndex - 1];
                    var v2 = teapotModel.Vertices[fv2.VertexIndex - 1];

                    // Determine normal: prefer provided vertex normal if available, else compute face normal
                    bool hasPerVertexNormals = teapotModel.Normals != null && teapotModel.Normals.Count > 0
                                               && fv0.NormalIndex > 0 && fv1.NormalIndex > 0 && fv2.NormalIndex > 0;

                    GL.Begin(PrimitiveType.Triangles);

                    if (hasPerVertexNormals)
                    {
                        var n0 = teapotModel.Normals[fv0.NormalIndex - 1];
                        GL.Normal3(n0.X, n0.Y, n0.Z);
                        GL.Vertex3(v0.X, v0.Y, v0.Z);

                        var n1 = teapotModel.Normals[fv1.NormalIndex - 1];
                        GL.Normal3(n1.X, n1.Y, n1.Z);
                        GL.Vertex3(v1.X, v1.Y, v1.Z);

                        var n2 = teapotModel.Normals[fv2.NormalIndex - 1];
                        GL.Normal3(n2.X, n2.Y, n2.Z);
                        GL.Vertex3(v2.X, v2.Y, v2.Z);
                    }
                    else
                    {
                        // compute face normal
                        float ux = v1.X - v0.X;
                        float uy = v1.Y - v0.Y;
                        float uz = v1.Z - v0.Z;

                        float vx = v2.X - v0.X;
                        float vy = v2.Y - v0.Y;
                        float vz = v2.Z - v0.Z;

                        float nx = uy * vz - uz * vy;
                        float ny = uz * vx - ux * vz;
                        float nz = ux * vy - uy * vx;

                        // normalize
                        float len = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                        if (len > 1e-6f)
                        {
                            nx /= len; ny /= len; nz /= len;
                        }

                        GL.Normal3(nx, ny, nz);
                        GL.Vertex3(v0.X, v0.Y, v0.Z);
                        GL.Vertex3(v1.X, v1.Y, v1.Z);
                        GL.Vertex3(v2.X, v2.Y, v2.Z);
                    }

                    GL.End();
                }
            }
        }

        GL.PopMatrix();
    }
}