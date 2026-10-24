using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace DualSpeaker {
 public class DetectedSide {public double BaseSeconds;public double[] TimesMs;public double[] Scores;public int GoodPulses;public double MeanScore;}
 public class Measurement {public bool Valid;public string Message;public float MicrophonePeak;public DetectedSide Left,Right;public double RightMinusLeftMs;public double SpreadMs;public double LeftDelayMs,RightDelayMs;public string Time;}
 public static class Calibration {
  static void Fft(double[] real,double[] imag,bool inverse){int n=real.Length;for(int i=1,j=0;i<n;i++){int bit=n>>1;for(;(j&bit)!=0;bit>>=1)j^=bit;j^=bit;if(i<j){double t=real[i];real[i]=real[j];real[j]=t;t=imag[i];imag[i]=imag[j];imag[j]=t;}}
   for(int len=2;len<=n;len<<=1){double a=2*Math.PI/len*(inverse?1:-1),wr=Math.Cos(a),wi=Math.Sin(a);for(int i=0;i<n;i+=len){double ur=1,ui=0;for(int j=0;j<len/2;j++){int p=i+j,q=p+len/2;double vr=real[q]*ur-imag[q]*ui,vi=real[q]*ui+imag[q]*ur;real[q]=real[p]-vr;imag[q]=imag[p]-vi;real[p]+=vr;imag[p]+=vi;double t=ur*wr-ui*wi;ui=ur*wi+ui*wr;ur=t;}}}
   if(inverse)for(int i=0;i<n;i++){real[i]/=n;imag[i]/=n;}
  }
  static double[] Correlate(float[] data,int rate,bool left){int m=(int)(TestSignal.Length*rate);int n=1;while(n<data.Length+m)n<<=1;double[] xr=new double[n],xi=new double[n],tr=new double[n],ti=new double[n],energy=new double[data.Length+1];double te=0;
   for(int i=0;i<data.Length;i++){xr[i]=data[i];energy[i+1]=energy[i]+data[i]*data[i];}for(int j=0;j<m;j++){double t=TestSignal.Chirp(left,j/(double)rate);tr[m-1-j]=t;te+=t*t;}
   Fft(xr,xi,false);
   double low=left?650:2100,high=left?2050:3950;
   for(int i=0;i<n;i++){double hz=Math.Min(i,n-i)*(double)rate/n;if(hz<low||hz>high){xr[i]=0;xi[i]=0;}}
   var er=(double[])xr.Clone();var ei=(double[])xi.Clone();Fft(er,ei,true);energy[0]=0;for(int i=0;i<data.Length;i++)energy[i+1]=energy[i]+er[i]*er[i];
   Fft(tr,ti,false);for(int i=0;i<n;i++){double r=xr[i]*tr[i]-xi[i]*ti[i];xi[i]=xr[i]*ti[i]+xi[i]*tr[i];xr[i]=r;}Fft(xr,xi,true);
   double[] score=new double[Math.Max(0,data.Length-m)];for(int i=0;i<score.Length;i++){double e=energy[i+m]-energy[i];score[i]=e<1e-10?0:Math.Abs(xr[i+m-1])/Math.Sqrt(e*te);}
   return score;
  }
  static DetectedSide Detect(float[] data,int rate,bool left){var c=Correlate(data,rate,left);int[] offsets=TestSignal.Offsets.Select(t=>(int)Math.Round(t*rate)).ToArray();int end=Math.Min((int)(rate*3.5),c.Length-offsets.Last()-1);if(end<=rate/5)throw new Exception("录音长度不足。");
   double best=-1;int start=0;for(int i=rate/5;i<end;i++){double sum=0;foreach(int k in offsets)sum+=c[i+k];if(sum>best){best=sum;start=i;}}
   var times=new double[offsets.Length];var scores=new double[offsets.Length];for(int j=0;j<offsets.Length;j++){int p=start+offsets[j],bestp=p;double value=c[p];for(int k=Math.Max(0,p-rate/40);k<Math.Min(c.Length,p+rate/40);k++){if(c[k]>value){value=c[k];bestp=k;}}times[j]=bestp*1000.0/rate;scores[j]=value;}
   return new DetectedSide{BaseSeconds=start/(double)rate,TimesMs=times,Scores=scores,GoodPulses=scores.Count(s=>s>=.28),MeanScore=scores.Average()};
  }
  static double Median(IEnumerable<double> data){double[] a=data.OrderBy(x=>x).ToArray();if(a.Length==0)return 0;return a.Length%2==1?a[a.Length/2]:(a[a.Length/2-1]+a[a.Length/2])/2;}
  public static Measurement Analyze(float[] samples,int sampleRate){int rate=12000;int count=(int)((long)samples.Length*rate/sampleRate);float[] a=new float[count];float max=0;for(int i=0;i<count;i++){double p=i*(double)sampleRate/rate;int k=(int)p;float x=samples[Math.Min(k,samples.Length-1)];if(k+1<samples.Length)x+=(samples[k+1]-x)*(float)(p-k);a[i]=x;max=Math.Max(max,Math.Abs(x));}
   var l=Detect(a,rate,true);var r=Detect(a,rate,false);var deltas=new List<double>();for(int i=0;i<l.TimesMs.Length;i++)if(l.Scores[i]>=.28&&r.Scores[i]>=.28)deltas.Add(r.TimesMs[i]-l.TimesMs[i]);double delta=Median(deltas),spread=Median(deltas.Select(v=>Math.Abs(v-delta)));
   bool valid=max>.0003&&deltas.Count>=4&&l.GoodPulses>=4&&r.GoodPulses>=4&&l.MeanScore>=.32&&r.MeanScore>=.32&&spread<18&&Math.Abs(delta)<1000;
   return new Measurement{Valid=valid,MicrophonePeak=max,Left=l,Right=r,RightMinusLeftMs=delta,SpreadMs=spread,Time=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),Message=valid?"录音识别成功":"录音未通过质量检查：请检查麦克风位置、音响音量及环境噪声；未使用此结果自动补偿。"};
  }
  public static Measurement Measure(AudioEngine engine,string label){if(!engine.Running)throw new Exception("请先启动双音响。");var samples=new List<float>();object gate=new object();Exception captureError=null;int rate;
   using(var mic=Devices.Open(engine.Settings.MicId))using(var capture=new WasapiCapture(mic)){
    if(mic.AudioEndpointVolume.Mute)throw new Exception("麦克风已静音。请先在 Windows 中解除麦克风静音。");RawMicrophone.Configure(capture);rate=capture.WaveFormat.SampleRate;
    capture.DataAvailable+=(s,e)=>{try{var a=AudioConvert.Decode(e.Buffer,e.BytesRecorded,capture.WaveFormat,true);lock(gate)samples.AddRange(a);}catch(Exception ex){captureError=ex;}};capture.RecordingStopped+=(s,e)=>{if(e.Exception!=null)captureError=e.Exception;};
    Files.Log("自动测量开始："+label+"，麦克风仅在此次校准中录音。format="+capture.WaveFormat);capture.StartRecording();Thread.Sleep(200);engine.EnableCalibration();try{for(int tick=0;tick<9;tick++){Thread.Sleep(1000);Files.Log("CAL_STATS | tick="+tick+" | input="+engine.Ring.Written+" | L="+engine.Left.FramesRead+","+engine.Left.LagMs.ToString("F1")+","+engine.Left.Peak.ToString("F3")+" | R="+engine.Right.FramesRead+","+engine.Right.LagMs.ToString("F1")+","+engine.Right.Peak.ToString("F3")+" | micFrames="+samples.Count);}}finally{engine.DisableCalibration();capture.StopRecording();Thread.Sleep(150);}
   }
   if(captureError!=null)throw captureError;float[] recorded;lock(gate)recorded=samples.ToArray();if(recorded.Length<rate*7)throw new Exception("麦克风录音过短或未返回数据。");
   string folder=Path.Combine(Files.Root,"diagnostics");Directory.CreateDirectory(folder);string stem=DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+label;
   using(var wave=new WaveFileWriter(Path.Combine(folder,stem+".wav"),WaveFormat.CreateIeeeFloatWaveFormat(rate,1))){wave.WriteSamples(recorded,0,recorded.Length);}
   var m=Analyze(recorded,rate);m.LeftDelayMs=engine.Settings.LeftDelayMs;m.RightDelayMs=engine.Settings.RightDelayMs;Files.Save(Path.Combine(folder,stem+".json"),m);
   Files.Log("测量结果 | valid="+m.Valid+" | right-left="+m.RightMinusLeftMs.ToString("F2")+" ms | MAD="+m.SpreadMs.ToString("F2")+" ms | scores="+m.Left.MeanScore.ToString("F3")+","+m.Right.MeanScore.ToString("F3")+" | peaks="+m.MicrophonePeak.ToString("F4"));return m;
  }
  public static Measurement Auto(AudioEngine engine,Action<string> progress){double oldLeft=engine.Settings.LeftDelayMs,oldRight=engine.Settings.RightDelayMs;bool saved=false;try{
   engine.Settings.LeftDelayMs=engine.Settings.RightDelayMs=0;engine.Settings.Calibrated=false;Thread.Sleep(5000);Measurement measurement=null;int consecutive=0;
   for(int attempt=0;attempt<6;attempt++){if(progress!=null)progress(attempt==0?"正在录音测量两台音响的时间差…":"正在录音复测补偿效果（"+attempt+"/5）…");measurement=Measure(engine,attempt==0?"baseline":"verify-"+attempt);if(!measurement.Valid)throw new Exception(measurement.Message);
    if(attempt>0&&Math.Abs(measurement.RightMinusLeftMs)<=15){consecutive++;if(consecutive>=2){engine.Settings.Calibrated=true;engine.Settings.LastResidualMs=measurement.RightMinusLeftMs;engine.Settings.CalibrationTime=measurement.Time;Files.Save(Files.Config,engine.Settings);saved=true;if(progress!=null)progress("连续两次校准通过：两路残差 "+measurement.RightMinusLeftMs.ToString("F1")+" ms");return measurement;}if(progress!=null)progress("首次对齐，继续录音确认稳定性…");Thread.Sleep(3000);continue;}else consecutive=0;
    if(attempt<5){double d=measurement.RightMinusLeftMs;engine.Settings.LeftDelayMs+=Math.Max(0,d);engine.Settings.RightDelayMs+=Math.Max(0,-d);double common=Math.Min(engine.Settings.LeftDelayMs,engine.Settings.RightDelayMs);engine.Settings.LeftDelayMs-=common;engine.Settings.RightDelayMs-=common;Thread.Sleep(3000);}
   }
   throw new Exception("已完成测量，但复测残差为 "+measurement.RightMinusLeftMs.ToString("F1")+" ms，尚未达到 15 ms 目标。没有将其标为校准通过。");
  }finally{engine.DisableCalibration();if(!saved){engine.Settings.LeftDelayMs=oldLeft;engine.Settings.RightDelayMs=oldRight;engine.Settings.Calibrated=false;Files.Save(Files.Config,engine.Settings);}}}
  public static void SelfTest(){int rate=48000;float[] a=new float[rate*9];for(int i=0;i<a.Length;i++){double t=i/(double)rate;a[i]=TestSignal.Value(true,t-1.2)+.7f*TestSignal.Value(false,t-1.437);}var m=Analyze(a,rate);if(!m.Valid||Math.Abs(m.RightMinusLeftMs-237)>1)throw new Exception("校准算法自检失败: "+m.RightMinusLeftMs);Files.Log("SELFTEST PASS | synthetic delay="+m.RightMinusLeftMs+" ms");var silence=Analyze(new float[rate*9],rate);if(silence.Valid)throw new Exception("静音错误地通过校准检查。");Files.Log("SELFTEST PASS | silence rejected");
   var random=new Random(42);for(int i=0;i<a.Length;i++)a[i]=(float)((random.NextDouble()-.5)*.02);var noise=Analyze(a,rate);if(noise.Valid)throw new Exception("噪声错误地通过校准检查。");Files.Log("SELFTEST PASS | random noise rejected");
   for(int i=0;i<a.Length;i++){double t=i/(double)rate;a[i]=.07f*TestSignal.Value(true,t-1.437)+TestSignal.Value(false,t-1.2);}var reverse=Analyze(a,rate);if(!reverse.Valid||Math.Abs(reverse.RightMinusLeftMs+237)>1)throw new Exception("反向或不等响度校准算法自检失败: "+reverse.RightMinusLeftMs);Files.Log("SELFTEST PASS | negative delay and unequal loudness");
   for(int i=0;i<a.Length;i++)a[i]=TestSignal.Value(true,i/(double)rate-1.2);if(Analyze(a,rate).Valid)throw new Exception("单音响错误地通过双音响校准检查。");Files.Log("SELFTEST PASS | missing speaker rejected");
   var ring=new AudioRing(48000);float[] stereo=new float[48000*2];for(int i=0;i<48000;i++){stereo[i*2]=.25f;stereo[i*2+1]=-.5f;}ring.Write(stereo,48000);var settings=new Settings{StereoSplit=true,LeftGain=1,RightGain=1};var left=new OutputProvider(ring,44100,true,settings);var right=new OutputProvider(ring,44100,false,settings);float[] b=new float[200];left.Read(b,0,b.Length);if(b.Any(v=>Math.Abs(v-.25)>.00001))throw new Exception("左声道分配错误。");right.Read(b,0,b.Length);if(b.Any(v=>Math.Abs(v+.5)>.00001))throw new Exception("右声道分配错误。");Files.Log("SELFTEST PASS | left/right channel routing at 44.1 kHz");
  }
 }
}
