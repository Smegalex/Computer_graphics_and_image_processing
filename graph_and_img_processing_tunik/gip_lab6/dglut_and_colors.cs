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

    // Load once at initialization - loads the embedded resource named "UtahTeapot" from the gip_lab6 resources.
    public void LoadTeapotFromResources(string resourceName = "UtahTeapot")
    {
        // Resource manager for the gip_lab6 Resources.resx
        var rm = new ResourceManager("gip_lab6.Resources", Assembly.GetExecutingAssembly());
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

        foreach (var group in teapotModel.Groups)
        {
            GL.Begin(PrimitiveType.Triangles);

            foreach (var face in group.Faces)
            {
                for (int i = 0; i < face.Count; i++)
                {
                    var faceVertex = face[i];

                    // Set normal if available
                    if (faceVertex.NormalIndex > 0 && teapotModel.Normals.Count > 0)
                    {
                        var normal = teapotModel.Normals[faceVertex.NormalIndex - 1];
                        GL.Normal3(normal.X, normal.Y, normal.Z);
                    }

                    // Set vertex
                    var vertex = teapotModel.Vertices[faceVertex.VertexIndex - 1];
                    GL.Vertex3(vertex.X, vertex.Y, vertex.Z);
                }
            }

            GL.End();
        }

        GL.PopMatrix();
    }
}