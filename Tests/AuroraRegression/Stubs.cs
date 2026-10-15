using System;
using SharpDX.Direct3D11;
using VRageMath;
namespace ClientPlugin { public class Plugin { public const string Name="AuroraRegression"; public static Plugin Instance => null; public string UpdateStatus => "fixture"; } }
namespace ClientPlugin.Settings {
 public static class PluginStatusPage { public static string RendererLine => "fixture"; public static string Status => "fixture"; public static string ProgramStatus(string id) => "fixture"; }
 public class PluginStatusScreen { public PluginStatusScreen(string text, string title) {} }
 public static class ConfigStorage { public static int Saves; public static ClientPlugin.Config Load()=>new ClientPlugin.Config(); public static void Save(ClientPlugin.Config c){Saves++;} }
}
namespace ClientPlugin.Settings.Elements {
 [AttributeUsage(AttributeTargets.Property|AttributeTargets.Method,AllowMultiple=true)] public class SeparatorAttribute(string label):Attribute;
 public class ButtonAttribute(string label=null,string description=null):Attribute;
 public class CheckboxAttribute(string description=null):Attribute;
 public class DropdownAttribute(string description=null):Attribute;
 public class ColorAttribute(string description=null):Attribute;
 public class SliderAttribute(float min,float max,float step,SliderAttribute.SliderType type,string label=null,string description=null):Attribute { public enum SliderType {Float,Integer} }
}
namespace Sandbox.Graphics.GUI { public static class MyGuiSandbox { public static void AddScreen(object screen) {} } }
namespace VRage.Utils {
 public class MyLog { public static readonly MyLog Default=new MyLog(); public int Errors; public void WriteLine(string s){} public void Warning(string s){} public void Error(string s){Errors++;Console.WriteLine(s);} }
}
namespace VRageRender {
 public static class MyRender11 {
  public static Device DeviceInstance;
  public static Env Environment=new Env();
  public class Env { public Matrices Matrices=new Matrices(); public Data Data=new Data(); }
  public class Matrices {public Vector3D CameraPosition=new Vector3D(0,0,150000);}
  public class Data {public Light EnvironmentLight=new Light();}
  public class Light {public Vector3 SunLightDirection=new Vector3(0,0,1);}
 }
 public static class MyCommon { public static Time FrameTime=new Time(); public class Time {public double Seconds=123.456;} }
}
namespace VRage.Render11.Resources {
 public interface ISrvBindable { string Name{get;} Resource Resource{get;} ShaderResourceView Srv{get;} Vector2I Size{get;} Vector3I Size3{get;} }
}
