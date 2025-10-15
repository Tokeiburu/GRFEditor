using System;
using System.Collections.Generic;
using System.Text;
using ErrorManager;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using TokeiLibrary;
using Utilities.Services;
using Matrix3 = OpenTK.Matrix3;
using Matrix4 = OpenTK.Matrix4;

namespace GRFEditor.OpenGL {
	public class Shader {
		public static StringBuilder ErrorOutput = new StringBuilder();
		public readonly int Handle;
		public static int LastHandle = -1;
		private readonly Dictionary<string, int> _uniformLocations;
		private string _vertPath;

		public string VertPath {
			get {
				return _vertPath;
			}
		}

		public Shader(string computePath) {
			_vertPath = computePath;

			var computerShaderData = ApplicationManager.GetResource(computePath);

			if (computerShaderData == null)
				throw new System.Exception("Unable to find resource " + computePath);

			var shaderSource = EncodingService.DisplayEncoding.GetString(computerShaderData);
			var computerShader = GL.CreateShader(ShaderType.ComputeShader);

			GL.ShaderSource(computerShader, shaderSource);

			CompileShader(computePath, computerShader);

			Handle = GL.CreateProgram();

			GL.AttachShader(Handle, computerShader);

			LinkProgram(Handle);

			GL.DetachShader(Handle, computerShader);
			GL.DeleteShader(computerShader);

			int numberOfUniforms;

			GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out numberOfUniforms);

			_uniformLocations = new Dictionary<string, int>();

			for (var i = 0; i < numberOfUniforms; i++) {
				var key = GL.GetActiveUniform(Handle, i, out _, out _);
				var location = GL.GetUniformLocation(Handle, key);

				_uniformLocations.Add(key, location);
			}

			if (ErrorOutput.Length > 0) {
				ErrorHandler.HandleException("Failed to load some shaders:\r\n\r\n" + Shader.ErrorOutput.ToString());
				ErrorOutput.Clear();
			}
		}

		public Shader(string vertPath, string fragPath) {
			_vertPath = vertPath;

			var vertShaderData = ApplicationManager.GetResource(vertPath);

			if (vertShaderData == null)
				throw new System.Exception("Unable to find resource " + vertPath);

			var shaderSource = EncodingService.DisplayEncoding.GetString(vertShaderData);
			var vertexShader = GL.CreateShader(ShaderType.VertexShader);

			GL.ShaderSource(vertexShader, shaderSource);

			CompileShader(vertPath, vertexShader);

			var fragShaderData = ApplicationManager.GetResource(fragPath);

			if (fragShaderData == null)
				throw new System.Exception("Unable to find resource " + fragPath);

			shaderSource = EncodingService.DisplayEncoding.GetString(fragShaderData);
			var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
			GL.ShaderSource(fragmentShader, shaderSource);
			CompileShader(fragPath, fragmentShader);

			Handle = GL.CreateProgram();

			GL.AttachShader(Handle, vertexShader);
			GL.AttachShader(Handle, fragmentShader);

			LinkProgram(Handle);

			GL.DetachShader(Handle, vertexShader);
			GL.DetachShader(Handle, fragmentShader);
			GL.DeleteShader(fragmentShader);
			GL.DeleteShader(vertexShader);

			int numberOfUniforms;

			GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out numberOfUniforms);

			_uniformLocations = new Dictionary<string, int>();

			for (var i = 0; i < numberOfUniforms; i++) {
				var key = GL.GetActiveUniform(Handle, i, out _, out _);
				var location = GL.GetUniformLocation(Handle, key);

				_uniformLocations.Add(key, location);
			}

			if (ErrorOutput.Length > 0) {
				ErrorHandler.HandleException("Failed to load some shaders:\r\n\r\n" + Shader.ErrorOutput.ToString());
				ErrorOutput.Clear();
			}
		}

		private static void CompileShader(string path, int shader) {
			GL.CompileShader(shader);

			int code;

			GL.GetShader(shader, ShaderParameter.CompileStatus, out code);
			if (code != (int)All.True) {
				var infoLog = GL.GetShaderInfoLog(shader);

				GLHelper.OnLog(() => "Error: " + infoLog);
				ErrorOutput.AppendLine(path);
				ErrorOutput.AppendLine(infoLog);
			}
		}

		private static void LinkProgram(int program) {
			GL.LinkProgram(program);

			int code;

			GL.GetProgram(program, GetProgramParameterName.LinkStatus, out code);
			if (code != (int)All.True) {
				GLHelper.OnLog(() => "Error: " + "Error occurred whilst linking Program({program})");
				ErrorOutput.AppendLine("Error occurred whilst linking Program({program})");
			}
		}

		public void Use() {
			if (LastHandle == Handle)
				return;

			GL.UseProgram(Handle);
			LastHandle = Handle;
		}

		public void Unuse() {
			GL.UseProgram(Handle);
		}

		public int GetAttribLocation(string attribName) {
			return GL.GetAttribLocation(Handle, attribName);
		}

		public void SetBool(string name, bool data) {
			if (!_uniformLocations.ContainsKey(name)) {
				return;
			}

			GL.Uniform1(_uniformLocations[name], data ? 1 : 0);
		}

		public void SetInt(string name, int data) {
			if (!_uniformLocations.ContainsKey(name)) {
				//Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.Uniform1(_uniformLocations[name], data);
		}

		public void SetFloat(string name, float data) {
			if (!_uniformLocations.ContainsKey(name)) {
				//Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.Uniform1(_uniformLocations[name], data);
		}

		public void Set(string name, double data) {
			if (!_uniformLocations.ContainsKey(name)) {
				//Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.Uniform1(_uniformLocations[name], data);
		}

		public void SetMatrix4(string name, ref Matrix4 data) {
			if (!_uniformLocations.ContainsKey(name)) {
				//Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.UniformMatrix4(_uniformLocations[name], true, ref data);
		}

		public void SetMatrix4(string name, Matrix4 data) {
			if (!_uniformLocations.ContainsKey(name)) {
				Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.UniformMatrix4(_uniformLocations[name], true, ref data);
		}

		public void SetMatrix3(string name, Matrix3 data) {
			if (!_uniformLocations.ContainsKey(name)) {
				//Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.UniformMatrix3(_uniformLocations[name], true, ref data);
		}

		public void SetVector3(string name, Vector3 data) {
			if (!_uniformLocations.ContainsKey(name)) {
				//Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.Uniform3(_uniformLocations[name], ref data);
		}

		public void SetVector3(string name, ref Vector3 data) {
			if (!_uniformLocations.ContainsKey(name)) {
				//Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.Uniform3(_uniformLocations[name], ref data);
		}

		public void SetVector2(string name, Vector2 data) {
			if (!_uniformLocations.ContainsKey(name)) {
				//Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.Uniform2(_uniformLocations[name], ref data);
		}

		public void SetVector2(string name, ref Vector2 data) {
			if (!_uniformLocations.ContainsKey(name)) {
				//Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.Uniform2(_uniformLocations[name], ref data);
		}

		public void SetVector4(string name, ref Vector4 data) {
			if (!_uniformLocations.ContainsKey(name)) {
				//Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.Uniform4(_uniformLocations[name], ref data);
		}

		public void SetVector4(string name, Vector4 data) {
			if (!_uniformLocations.ContainsKey(name)) {
				//Console.WriteLine("Warning: property '" + name + "' not found in the shader.");
				return;
			}

			GL.Uniform4(_uniformLocations[name], ref data);
		}
	}
}
