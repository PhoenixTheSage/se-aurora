using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ClientPlugin;
using ClientPlugin.Anomaly;
using ClientPlugin.Aurora;
using ClientPlugin.RichHud;
using ClientPlugin.Settings;
using ClientPlugin.Shaders;
using ClientPlugin.Buffers;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRageMath;
using VRageRender;

static class Program
{
 static int assertions;
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void ColorTransitions()
 {
  var setter=typeof(AnomalyTerminalHook).GetMethod("SetCustomColor",BindingFlags.NonPublic|BindingFlags.Static);
  foreach(AuroraColorPreset preset in Enum.GetValues(typeof(AuroraColorPreset)))
  {
   if(preset==AuroraColorPreset.Custom)continue;
   foreach(bool bottom in new[]{true,false})
   {
    Config.Current.BottomColor=new Color(1,2,3);Config.Current.TopColor=new Color(4,5,6);Config.Current.ColorPreset=preset;
    Config.Current.GetGradientColors(out var beforeBottom,out var beforeTop);var edit=new Color(40,120,200);int saves=ConfigStorage.Saves;
    setter.Invoke(null,new object[]{bottom,edit});
    Check(Config.Current.ColorPreset==AuroraColorPreset.Custom,"Picker did not select Custom");
    Check((bottom?Config.Current.BottomColor:Config.Current.TopColor)==edit,"Edited color was lost");
    Check((bottom?Config.Current.TopColor:Config.Current.BottomColor)==new Color(bottom?beforeTop:beforeBottom),"Untouched displayed endpoint changed: "+preset);
    Check(ConfigStorage.Saves==saves+1,"Preset edit must request one deferred save");
   }
  }
  Config.Current.ColorPreset=AuroraColorPreset.Custom;var storedTop=Config.Current.TopColor;setter.Invoke(null,new object[]{true,new Color(2,7,11)});
  Check(Config.Current.TopColor==storedTop,"Existing custom endpoint changed");
 }
 sealed class LostException:Exception {public LostException(){HResult=unchecked((int)0x887A0006);}}
 static void Main()
 {
  ColorTransitions();
  using(var device=new Device(DriverType.Hardware,DeviceCreationFlags.None,FeatureLevel.Level_11_0))
  {
   MyRender11.DeviceInstance=device;
   var timer=Stopwatch.StartNew();bool ready=AuroraTextures.EnsureCreated(Config.Current);timer.Stop();
   Check(timer.ElapsedMilliseconds<100,"Render callback blocked on initial noise bake");
   var task=(Task<byte[]>)typeof(AuroraTextures).GetField("noisePixels",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
   Check(task.Wait(10000),"Noise worker did not finish");
   using(var sha=SHA256.Create())Check(BitConverter.ToString(sha.ComputeHash(task.Result)).Replace("-","")=="ED288ACB396B93F6445FB454DA8A009D2E0930203609F1EC61C588D3E849FF4D","Noise bytes changed");
   Check(AuroraTextures.EnsureCreated(Config.Current),"Initial texture pair did not become ready");
   var originalNoise=AuroraTextures.Noise;var originalRamp=AuroraTextures.Ramp;
   Check(AnomalyBridge.TryRegisterPack("test-pack"),"Bridge registration failed");
   Check(AnomalyBridge.VelocityDistanceScale==1000&&FullscreenPassRegistry.DistanceScale==1000,"Host decode/pack encode scale mismatch");
   Check(AnomalyBridge.PublishTextures(),"Initial catalog publication failed");int publishes=BufferCatalog.PublishCalls;
   Check(AnomalyBridge.PublishTextures()&&BufferCatalog.PublishCalls==publishes,"Unchanged textures republished");
   Check(AnomalyBridge.SetPassEnabled(true),"Pass enable failed");int enables=FullscreenPassRegistry.EnabledCalls;
   Check(AnomalyBridge.SetPassEnabled(true)&&FullscreenPassRegistry.EnabledCalls==enables,"Unchanged enabled state renotified");
   for(int i=0;i<20;i++){BufferCatalog.ResolutionChanged();Check(AuroraTextures.EnsureCreated(Config.Current),"DRS made pixels pending");Check(ReferenceEquals(originalNoise,AuroraTextures.Noise)&&ReferenceEquals(originalRamp,AuroraTextures.Ramp),"DRS recreated fixed textures");}
   Check(!originalNoise.Srv.IsDisposed&&!originalRamp.Srv.IsDisposed,"DRS retired a live texture");
   Config.Current.NightOnly=false;AuroraRenderer.Publish(new AuroraSnapshot(Vector3D.Zero,61500,63000,Vector3.UnitY,1,800000,1000000,60000));
   AuroraRenderer.PushUniforms();Check(FullscreenPassRegistry.Enabled&&FullscreenPassRegistry.Uniforms[35]==1000,"Renderer did not enable matched distance contract");
   AuroraTextures.MarkRampDirty();MyRender11.DeviceInstance=null;int errors=VRage.Utils.MyLog.Default.Errors;
   AuroraRenderer.PushUniforms();
   Check(!FullscreenPassRegistry.Enabled&&FullscreenPassRegistry.Uniforms.All(v=>v==0),"Failed setup retained enabled state/uniforms");
   Check(ReferenceEquals(originalRamp,AuroraTextures.Ramp)&&!originalRamp.Srv.IsDisposed,"Failed replacement destroyed last good ramp");
   AuroraRenderer.PushUniforms();Check(VRage.Utils.MyLog.Default.Errors==errors+1,"Fault loop retried/logged again");
   MyRender11.DeviceInstance=device;BufferCatalog.ResolutionChanged();AuroraRenderer.PushUniforms();
   Check(FullscreenPassRegistry.Enabled&&!ReferenceEquals(originalRamp,AuroraTextures.Ramp)&&originalRamp.Srv.IsDisposed,"Recovery did not replace ramp after success");
   FullscreenPassRegistry.UniformFailure=new LostException();bool lost=false;
   try{AuroraRenderer.PushUniforms();}catch(LostException){lost=true;}
   Check(lost,"Genuine DXGI error was swallowed");FullscreenPassRegistry.UniformFailure=null;
   Check(RenderTraceBind.IsLostDevice(new AggregateException(new Exception("ordinary"),new LostException())),"Nested device loss not recognized");
   originalNoise=AuroraTextures.Noise;var oldRamp=AuroraTextures.Ramp;BufferCatalog.DeviceEnd();
   Check(originalNoise.Srv.IsDisposed&&oldRamp.Srv.IsDisposed&&BufferCatalog.Entries.Count==0,"Device-end resource cleanup failed");
   timer.Restart();Check(AuroraTextures.EnsureCreated(Config.Current),"Device recreation rebaked pixels");timer.Stop();
   Check(ReferenceEquals(task,typeof(AuroraTextures).GetField("noisePixels",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null)),"Device-end replaced CPU noise task");
   Console.WriteLine("Cached device reupload: "+timer.Elapsed.TotalMilliseconds.ToString("F3")+" ms; initial callback: "+ready);
   // A host that declines the optional contract must keep raw-metre uniforms.
   FullscreenPassRegistry.AcceptScale=false;typeof(AnomalyBridge).GetField("registered",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,false);
   Check(AnomalyBridge.TryRegisterPack("test-pack")&&AnomalyBridge.VelocityDistanceScale==1,"Older/rejecting host did not fall back to metres");
   AuroraRenderer.PushUniforms();Check(FullscreenPassRegistry.Uniforms[35]==1,"Fallback encoded kilometre alpha without host decode");
   AuroraRenderer.Publish(null);AuroraRenderer.PushUniforms();Check(!FullscreenPassRegistry.Enabled,"Session teardown left pass enabled");AnomalyBridge.ReleaseTextures();
  }
  Console.WriteLine("PASS: "+assertions+" Aurora regression assertions, runtime "+Environment.Version);
 }
}
