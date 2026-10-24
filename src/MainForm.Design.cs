using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DualSpeaker {
 public partial class MainForm {
  [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);try{int dark=1;DwmSetWindowAttribute(Handle,20,ref dark,4);}catch{}try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}}
  StudioMeter leftMeter,rightMeter;StatusLamp playLamp,protectionLamp;Label leftTrimText,rightTrimText,leftDeviceVolumeText,rightDeviceVolumeText;
  Panel pageHost;Control[] pages;StudioButton[] tabs;System.Windows.Forms.Timer instrumentTimer;ToolTip tips;
  bool preview;int currentPage=-1;UniformConsoleLayout uniformLayout;bool freezingLayout;internal float VisualZoom{get{return uniformLayout==null?1:uniformLayout.Zoom;}}
  public MainForm(bool auto,bool fullTest):this(auto,fullTest,false){}
  public MainForm(bool auto,bool fullTest,bool designPreview){
   preview=designPreview;autoStart=auto;commission=fullTest;settings=Devices.Initial();
   SuspendLayout();Text="双蓝牙音响 · DUALSPEAKER";AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(1120,790);MinimumSize=new Size(1060,795);StartPosition=FormStartPosition.CenterScreen;
   Font=Studio.Font(9,FontStyle.Regular);BackColor=Studio.Background;ForeColor=Studio.Text;DoubleBuffered=true;
   BuildInterface();ResumeLayout(true);WireInterface();RefreshDevices();RefreshMasterVolume();UpdateStatus();
   Shown+=(s,e)=>InitializeUniformLayout();Resize+=(s,e)=>{if(uniformLayout!=null)uniformLayout.Apply();};DpiChanged+=(s,e)=>{if(uniformLayout!=null)BeginInvoke(new Action(()=>uniformLayout.Apply()));};
   if(preview)return;
   timer=new System.Windows.Forms.Timer{Interval=500};timer.Tick+=(s,e)=>UpdateStatus();timer.Start();
   instrumentTimer=new System.Windows.Forms.Timer{Interval=33};instrumentTimer.Tick+=(s,e)=>UpdateInstruments();instrumentTimer.Start();
   FormClosing+=(s,e)=>{if(busy){e.Cancel=true;status.Text="正在测量或启动，请等待完成后关闭。";}else{timer.Stop();instrumentTimer.Stop();StopPlayback();}};
   Shown+=async(s,e)=>{try{mediaKeys=new MediaVolumeKeys(()=>engine!=null&&engine.Running,key=>{if(IsHandleCreated&&!IsDisposed)BeginInvoke(new Action(()=>ApplyVolumeKey(key)));});Append("键盘旋钮已接入：音量加减控制双音响总音量，静音键控制两路静音。");}catch(Exception ex){Append(ex.Message);}if(autoStart)await StartPlayback();if(commission&&engine!=null&&engine.Running)await Commission();};
  }
  protected override void Dispose(bool disposing){if(disposing){if(uniformLayout!=null)uniformLayout.Dispose();if(timer!=null)timer.Dispose();if(instrumentTimer!=null)instrumentTimer.Dispose();if(mediaKeys!=null)mediaKeys.Dispose();if(volumeOverlay!=null)volumeOverlay.Dispose();if(tips!=null)tips.Dispose();}base.Dispose(disposing);}
  static Label LabelAt(string text,float size,Color color){return new StudioLabel{Text=text,Font=Studio.Font(size,FontStyle.Regular),ForeColor=color,BackColor=Color.Transparent,AutoSize=false,TextAlign=ContentAlignment.MiddleLeft,Margin=Padding.Empty};}
  static StudioButton Button(string text,int width){return new StudioButton{Text=text,Width=width,Height=36,Margin=new Padding(0,0,10,0)};}
  static ComboBox Combo(){return new StudioCombo{Dock=DockStyle.Fill};}
  static NumericUpDown Number(decimal minimum,decimal maximum,decimal value,int width,string name){return new StudioNumber{Minimum=minimum,Maximum=maximum,Value=Math.Max(minimum,Math.Min(maximum,value)),Width=width,Name=name,BackColor=Studio.Inset,ForeColor=Studio.Text,BorderStyle=BorderStyle.FixedSingle,TextAlign=HorizontalAlignment.Center,Font=new Font("Consolas",11),Margin=new Padding(0,5,0,0)};}
  static void Place(Control p,Control c,int x,int y,int w,int h){c.SetBounds(x,y,w,h);p.Controls.Add(c);}
  static Panel Surface(){return new StudioSurface{Dock=DockStyle.Fill,BackColor=Studio.Background};}
  static TableLayoutPanel Grid(int columns){var p=new StudioLayout{Dock=DockStyle.Fill,ColumnCount=columns,Margin=Padding.Empty,Padding=Padding.Empty,BackColor=Color.Transparent};for(int i=0;i<columns;i++)p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/columns));return p;}
  void BuildInterface(){
   tips=new ToolTip{BackColor=Studio.Panel,ForeColor=Studio.Text,AutoPopDelay=6000};
   var shell=new StudioLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(22,0,22,0),BackColor=Studio.Background};shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,88));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,52));shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,38));Controls.Add(shell);
   var header=Surface();header.Margin=Padding.Empty;shell.Controls.Add(header,0,0);
   var wordmark=new StudioWordmark{Name="brandWordmark"};Place(header,wordmark,0,10,300,64);
   var title=LabelAt("双音响控制台",15,Studio.Text);title.Font=Studio.Medium(15);Place(header,title,333,20,390,29);Place(header,LabelAt("桌面音频 · 双路独立调节",8.5f,Studio.Dim),335,52,390,23);
   var edition=LabelAt("双路蓝牙 · 桌面音频",9,Studio.Dim);Place(header,edition,837,17,235,24);
   playLamp=new StatusLamp{Text="待机 · STANDBY"};Place(header,playLamp,837,45,235,25);
   Action layoutHeader=()=>{if(freezingLayout||uniformLayout!=null)return;int x=header.Width-HighDpi.Pixels(239,header);edition.SetBounds(x,HighDpi.Pixels(17,header),HighDpi.Pixels(235,header),HighDpi.Pixels(24,header));playLamp.SetBounds(x,HighDpi.Pixels(45,header),HighDpi.Pixels(235,header),HighDpi.Pixels(25,header));};header.Layout+=(s,e)=>layoutHeader();Shown+=(s,e)=>layoutHeader();
   var nav=Surface();nav.Margin=Padding.Empty;shell.Controls.Add(nav,0,1);tabs=new StudioButton[3];string[] names={"播放控制","设备与校准","运行日志"};for(int i=0;i<3;i++){int index=i;tabs[i]=Button(names[i],146);tabs[i].Font=Studio.Medium(10);tabs[i].Name="pageTab"+i;tabs[i].Click+=(s,e)=>ShowPage(index);Place(nav,tabs[i],i*158,0,146,38);}
   var slogan=LabelAt("T W O   S P E A K E R S .   O N E   D E S K .",7,Studio.Dim);Place(nav,slogan,668,0,408,38);Action layoutSlogan=()=>{if(freezingLayout||uniformLayout!=null)return;slogan.SetBounds(nav.Width-HighDpi.Pixels(408,nav),0,HighDpi.Pixels(408,nav),HighDpi.Pixels(38,nav));};nav.Layout+=(s,e)=>layoutSlogan();Shown+=(s,e)=>layoutSlogan();
   pageHost=Surface();pageHost.Margin=Padding.Empty;shell.Controls.Add(pageHost,0,2);
   left=Combo();right=Combo();bus=Combo();mic=Combo();mode=Combo();mode.Items.AddRange(new object[]{"双音响 · 两台播放完整立体声","左右声道 · 左右音响各播放一个声道"});mode.SelectedIndex=settings.StereoSplit?1:0;
   latencyMode=Combo();latencyMode.Items.AddRange(new object[]{"视频优先 · 较小缓冲","音乐优先 · 较大缓冲"});latencyMode.SelectedIndex=settings.BufferMs<=120?0:1;
   start=Button("开始播放",126);((StudioButton)start).Accent=true;stop=Button("停止并恢复",126);calibrate=Button("自动校准",135);test=Button("播放验证音",135);refresh=Button("刷新设备",135);
   masterVolume=new StudioKnob{Minimum=0,Maximum=100,Value=(int)Math.Round(Math.Max(0,Math.Min(1,settings.MasterGain))*100),Name="masterVolume"};masterVolumeText=new StudioReadout{Name="masterReadout",Font=new Font("Consolas",26)};masterMute=Button("静音",94);
   knobSensitivity=Number(1,20,settings.VolumeStepPercent,80,"knobSensitivity");knobSensitivity.AccessibleName="旋钮灵敏度";
   leftDeviceVolume=new StudioFader{Minimum=0,Maximum=100,Name="leftDeviceVolume",AccessibleName="左音响设备音量"};rightDeviceVolume=new StudioFader{Minimum=0,Maximum=100,Name="rightDeviceVolume",AccessibleName="右音响设备音量"};
   leftDeviceMute=LabelAt("",8,Studio.Amber);rightDeviceMute=LabelAt("",8,Studio.Amber);
   leftGain=new StudioFader{Minimum=0,Maximum=100,Value=(int)(settings.LeftGain*100),AccessibleName="左音响软件音量"};rightGain=new StudioFader{Minimum=0,Maximum=100,Value=(int)(settings.RightGain*100),AccessibleName="右音响软件音量"};
   leftDelay=Number(0,2000,(decimal)settings.LeftDelayMs,86,"leftDelay");rightDelay=Number(0,2000,(decimal)settings.RightDelayMs,86,"rightDelay");leftDelay.DecimalPlaces=rightDelay.DecimalPlaces=1;leftDelay.AccessibleName="左音响延迟补偿（毫秒）";rightDelay.AccessibleName="右音响延迟补偿（毫秒）";
   status=LabelAt("尚未启动",9,Studio.Dim);status.Dock=DockStyle.Fill;status.AutoEllipsis=true;status.Margin=Padding.Empty;var footer=Grid(2);footer.ColumnStyles.Clear();footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,252));footer.Controls.Add(status,0,0);protectionLamp=new StatusLamp{Text="笔记本静音保护 · 待机",Dock=DockStyle.Fill,BackColor=Color.Transparent,Font=Studio.Font(8,FontStyle.Regular)};footer.Controls.Add(protectionLamp,1,0);shell.Controls.Add(footer,0,3);
   calibrationInfo=LabelAt("",9,Studio.Dim);meters=LabelAt("",9,Studio.Dim);
   log=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BackColor=Studio.Inset,ForeColor=Studio.Text,BorderStyle=BorderStyle.None,Font=new Font("Segoe UI",10),Margin=new Padding(0,14,0,0)};
   autoCalibration=new StudioCheck{Text="启动时自动校准",Description="每次开始播放时，用麦克风测量两路时间差",Checked=settings.CalibrateOnStart,Name="autoCalibration"};
   pages=new Control[]{BuildConsole(),BuildSettings(),BuildLog()};foreach(var page in pages){pageHost.Controls.Add(page);page.Visible=false;}ShowPage(0);
   tips.SetToolTip(masterVolume,"向上拖动、滚轮或方向键调节音量。键盘旋钮仍可直接控制两台音响。");tips.SetToolTip(leftDelay,"只为该路增加播放延迟，单位为毫秒。");tips.SetToolTip(rightDelay,"只为该路增加播放延迟，单位为毫秒。");tips.SetToolTip(leftGain,"100% 表示原样输出；音响设备音量在上方独立控制。");tips.SetToolTip(rightGain,"100% 表示原样输出；音响设备音量在上方独立控制。");tips.SetToolTip(leftDeviceVolume,"直接控制左音响的 Windows 设备音量。支持拖动、点击、滚轮和方向键。");tips.SetToolTip(rightDeviceVolume,"直接控制右音响的 Windows 设备音量。支持拖动、点击、滚轮和方向键。");
  }
  Control BuildConsole(){
   var page=Grid(1);page.RowCount=3;page.RowStyles.Add(new RowStyle(SizeType.Percent,50));page.RowStyles.Add(new RowStyle(SizeType.Percent,50));page.RowStyles.Add(new RowStyle(SizeType.Absolute,75));
   var instrument=new MetalPanel{Dock=DockStyle.Fill,Screws=true,Margin=new Padding(0,0,0,14)};page.Controls.Add(instrument,0,0);
   var instruments=Grid(2);instruments.Padding=new Padding(25,16,23,12);instruments.ColumnStyles.Clear();instruments.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,67));instruments.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33));instrument.Controls.Add(instruments);
   var monitoring=Grid(1);monitoring.Padding=new Padding(0,0,25,0);monitoring.RowCount=2;monitoring.RowStyles.Add(new RowStyle(SizeType.Absolute,31));monitoring.RowStyles.Add(new RowStyle(SizeType.Percent,100));instruments.Controls.Add(monitoring,0,0);
   var meterTitle=LabelAt("实时输出电平   /   OUTPUT MONITOR",8,Studio.Dim);meterTitle.Dock=DockStyle.Fill;monitoring.Controls.Add(meterTitle,0,0);
   var meterPair=Grid(2);meterPair.Padding=new Padding(0,4,0,6);monitoring.Controls.Add(meterPair,0,1);leftMeter=new StudioMeter{Dock=DockStyle.Fill,Channel="L",Margin=new Padding(0,0,9,0)};rightMeter=new StudioMeter{Dock=DockStyle.Fill,Channel="R",Margin=new Padding(9,0,0,0)};meterPair.Controls.Add(leftMeter,0,0);meterPair.Controls.Add(rightMeter,1,0);

   var master=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};instruments.Controls.Add(master,1,0);
   var mt=LabelAt("总音量  /  MASTER VOLUME",8,Studio.Dim);mt.TextAlign=ContentAlignment.MiddleCenter;mt.Dock=DockStyle.Top;mt.Height=24;master.Controls.Add(mt);
   master.Controls.Add(masterVolume);master.Controls.Add(masterVolumeText);master.Controls.Add(masterMute);
   master.Resize+=(s,e)=>LayoutMaster(master);
   master.DpiChangedAfterParent+=(s,e)=>LayoutMaster(master);
   var channels=Grid(2);channels.Margin=Padding.Empty;channels.ColumnStyles.Clear();channels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));channels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));page.Controls.Add(channels,0,1);
   channels.Controls.Add(BuildChannel(true),0,0);channels.Controls.Add(BuildChannel(false),1,0);
   var transport=Grid(3);transport.ColumnStyles.Clear();transport.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,286));transport.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,80));transport.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));transport.Padding=new Padding(0,16,0,8);page.Controls.Add(transport,0,2);
   var buttons=new StudioFlowLayout{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty,BackColor=Color.Transparent};buttons.Controls.Add(start);buttons.Controls.Add(stop);transport.Controls.Add(buttons,0,0);var ml=LabelAt("播放模式",9,Studio.Dim);ml.Dock=DockStyle.Fill;ml.TextAlign=ContentAlignment.TopLeft;ml.Padding=new Padding(0,10,0,0);transport.Controls.Add(ml,1,0);mode.Margin=new Padding(0,0,0,0);transport.Controls.Add(mode,2,0);
   return page;
  }
  void LayoutMaster(Panel master){if(freezingLayout||uniformLayout!=null)return;float scale=HighDpi.Scale(master);int knob=(int)Math.Round(168*scale),displayWidth=(int)Math.Round(137*scale),gap=(int)Math.Round(10*scale);int left=Math.Max(0,(master.Width-knob-displayWidth-gap)/2),top=Math.Max((int)Math.Round(32*scale),(master.Height-knob)/2+(int)Math.Round(8*scale));masterVolume.SetBounds(left,top,knob,knob);int x=left+knob+gap;masterVolumeText.SetBounds(x,top+(int)Math.Round(23*scale),displayWidth,(int)Math.Round(75*scale));masterMute.SetBounds(x,top+(int)Math.Round(112*scale),displayWidth,(int)Math.Round(34*scale));}
  Control BuildChannel(bool isLeft){
   var card=new MetalPanel{Dock=DockStyle.Fill,Screws=true,Margin=isLeft?new Padding(0,0,7,0):new Padding(7,0,0,0),Padding=new Padding(20,16,20,16)};
   var table=Grid(1);table.RowCount=6;foreach(int height in new[]{27,35,36,36,36})table.RowStyles.Add(new RowStyle(SizeType.Absolute,height));table.RowStyles.Add(new RowStyle(SizeType.Percent,100));card.Controls.Add(table);
   var heading=LabelAt(isLeft?"01   /   左侧音响":"02   /   右侧音响",11,Studio.Text);heading.Font=Studio.Medium(11);heading.Dock=DockStyle.Fill;table.Controls.Add(heading,0,0);var combo=isLeft?left:right;combo.Margin=new Padding(0,0,0,6);table.Controls.Add(combo,0,1);
   Label deviceText,trimText;
   table.Controls.Add(BuildLevelRow("设备音量",isLeft?leftDeviceVolume:rightDeviceVolume,out deviceText),0,2);
   table.Controls.Add(BuildLevelRow("软件音量",isLeft?leftGain:rightGain,out trimText),0,3);
   if(isLeft){leftDeviceVolumeText=deviceText;leftTrimText=trimText;}else{rightDeviceVolumeText=deviceText;rightTrimText=trimText;}
   var timing=Grid(4);timing.ColumnStyles.Clear();timing.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,76));timing.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,88));timing.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,27));timing.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   var label=LabelAt("延迟补偿",8,Studio.Dim);label.Dock=DockStyle.Fill;timing.Controls.Add(label,0,0);var delay=isLeft?leftDelay:rightDelay;delay.Margin=new Padding(0,6,0,0);timing.Controls.Add(delay,1,0);var unit=LabelAt("ms",8,Studio.Dim);unit.Dock=DockStyle.Fill;timing.Controls.Add(unit,2,0);var muted=isLeft?leftDeviceMute:rightDeviceMute;muted.Dock=DockStyle.Fill;muted.TextAlign=ContentAlignment.MiddleRight;timing.Controls.Add(muted,3,0);table.Controls.Add(timing,0,4);table.Controls.Add(new SpeakerGrille{Dock=DockStyle.Fill,Margin=new Padding(0,8,0,0)},0,5);return card;
  }
  static Control BuildLevelRow(string title,StudioValue slider,out Label readout){
   var row=Grid(3);row.ColumnStyles.Clear();row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,76));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,50));
   var label=LabelAt(title,8,Studio.Dim);label.Dock=DockStyle.Fill;row.Controls.Add(label,0,0);slider.Dock=DockStyle.Fill;slider.Margin=Padding.Empty;row.Controls.Add(slider,1,0);readout=LabelAt(slider.Value+"%",9,Studio.Text);readout.Font=new Font("Segoe UI",10);readout.Dock=DockStyle.Fill;readout.TextAlign=ContentAlignment.MiddleRight;row.Controls.Add(readout,2,0);return row;
  }
  Control BuildSettings(){
   var page=Grid(2);page.Padding=new Padding(0,0,0,12);
   var source=new MetalPanel{Dock=DockStyle.Fill,Screws=true,Padding=new Padding(22),Margin=new Padding(0,0,7,0)};page.Controls.Add(source,0,0);
   var audio=SettingsTable(source,"01","音源与操控","SOURCE  /  CONTROL");
   SettingsWide(audio,new StudioDivider{Text="音源设置"},26);SettingsRow(audio,"电脑音源",bus,52);SettingsRow(audio,"播放缓冲",latencyMode,52);
   SettingsWide(audio,new StudioDivider{Text="操控偏好"},30);SettingsRow(audio,"旋钮步长",Inline(knobSensitivity,LabelAt("% / 步",9,Studio.Dim)),46);
   var showOverlay=new StudioCheck{Text="音量浮窗",Description="调节音量时显示提示，最小化后也可见",Checked=settings.ShowVolumeOverlay,Name="showVolumeOverlay"};SettingsWide(audio,showOverlay,68);showOverlay.CheckedChanged+=(s,e)=>{settings.ShowVolumeOverlay=showOverlay.Checked;if(!showOverlay.Checked&&volumeOverlay!=null)volumeOverlay.Dismiss();Save();};
   SettingsWide(audio,SettingsNote("键盘与扬声器",LabelAt("转动键盘旋钮调节总音量，按下可静音。\n播放时笔记本保持静音，停止后恢复。",9,Studio.Dim)),100);
   var refreshRow=new StudioFlowLayout{Dock=DockStyle.Fill,Margin=Padding.Empty,Padding=new Padding(0,8,0,0),BackColor=Color.Transparent,WrapContents=false};refreshRow.Controls.Add(refresh);refresh.Text="刷新设备列表";SettingsWide(audio,refreshRow,48);
   var calibration=new MetalPanel{Dock=DockStyle.Fill,Screws=true,Padding=new Padding(22),Margin=new Padding(7,0,0,0)};page.Controls.Add(calibration,1,0);var ct=SettingsTable(calibration,"02","延迟与校准","TIMING  /  CALIBRATION");
   SettingsWide(ct,new StudioDivider{Text="测量设置"},26);SettingsRow(ct,"校准麦克风",mic,52);SettingsWide(ct,autoCalibration,68);
   SettingsWide(ct,new StudioDivider{Text="校准操作"},30);var cb=new StudioFlowLayout{Dock=DockStyle.Fill,Margin=Padding.Empty,Padding=new Padding(0,3,0,0),BackColor=Color.Transparent,WrapContents=false};cb.Controls.Add(calibrate);cb.Controls.Add(test);((StudioButton)calibrate).Accent=true;calibrate.Text="开始自动校准";SettingsWide(ct,cb,48);
   calibrationInfo.TextAlign=ContentAlignment.TopLeft;SettingsWide(ct,SettingsNote("校准记录",calibrationInfo),100);
   SettingsWide(ct,SettingsNote("听音位置与时间",LabelAt("测量约需 35–80 秒，期间使用麦克风。\n移动音响或重连后，可以重新校准。\n两路时间补偿不等于消除视频口型延迟。",9,Studio.Dim)),100);
   SettingsEnd(audio);SettingsEnd(ct);
   return page;
  }
  static void SettingsEnd(TableLayoutPanel p){p.RowCount++;p.RowStyles.Add(new RowStyle(SizeType.Percent,100));}
  static TableLayoutPanel SettingsTable(Control parent,string number,string title,string sub){var p=new StudioLayout{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty,BackColor=Color.Transparent};p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,96));p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));parent.Controls.Add(p);SettingsWide(p,new StudioHeading{Number=number,Text=title,Caption=sub},60);return p;}
  static Control SettingsNote(string title,Label body){var panel=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty,Padding=new Padding(13,8,13,8),BackColor=Color.FromArgb(38,44,37)};var table=Grid(1);table.RowCount=2;table.RowStyles.Add(new RowStyle(SizeType.Absolute,24));table.RowStyles.Add(new RowStyle(SizeType.Percent,100));panel.Controls.Add(table);var caption=LabelAt(title,8,Studio.Amber);caption.Dock=DockStyle.Fill;table.Controls.Add(caption,0,0);body.Dock=DockStyle.Fill;body.TextAlign=ContentAlignment.TopLeft;table.Controls.Add(body,0,1);var holder=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty,Padding=new Padding(0,5,0,5),BackColor=Color.Transparent};holder.Controls.Add(panel);return holder;}
  static void SettingsWide(TableLayoutPanel p,Control c,int h){int row=p.RowCount++;p.RowStyles.Add(new RowStyle(SizeType.Absolute,h));p.Controls.Add(c,0,row);p.SetColumnSpan(c,2);c.Dock=DockStyle.Fill;c.Margin=Padding.Empty;}
  static void SettingsRow(TableLayoutPanel p,string text,Control control,int h){int row=p.RowCount++;p.RowStyles.Add(new RowStyle(SizeType.Absolute,h));var label=LabelAt(text,9,Studio.Dim);label.Dock=DockStyle.Fill;p.Controls.Add(label,0,row);var holder=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty,Padding=new Padding(0,13,0,0),BackColor=Color.Transparent};holder.Controls.Add(control);control.Dock=DockStyle.Top;p.Controls.Add(holder,1,row);}
  static Control Inline(Control first,Label suffix){var p=new StudioFlowLayout{Height=34,Dock=DockStyle.Fill,Margin=Padding.Empty,BackColor=Color.Transparent};first.Margin=Padding.Empty;suffix.Size=new Size(100,28);suffix.Margin=new Padding(8,0,0,0);p.Controls.Add(first);p.Controls.Add(suffix);return p;}
  Control BuildLog(){var page=new MetalPanel{Dock=DockStyle.Fill,Screws=true,Padding=new Padding(25),Margin=new Padding(0,0,0,12)};var table=Grid(1);table.RowCount=4;table.RowStyles.Add(new RowStyle(SizeType.Absolute,42));table.RowStyles.Add(new RowStyle(SizeType.Absolute,90));table.RowStyles.Add(new RowStyle(SizeType.Absolute,30));table.RowStyles.Add(new RowStyle(SizeType.Percent,100));page.Controls.Add(table);var title=LabelAt("运行状态",16,Studio.Text);title.Dock=DockStyle.Fill;table.Controls.Add(title,0,0);meters.Dock=DockStyle.Fill;table.Controls.Add(meters,0,1);var caption=LabelAt("事件记录   /   SESSION LOG",8,Studio.Amber);caption.Dock=DockStyle.Fill;table.Controls.Add(caption,0,2);table.Controls.Add(log,0,3);return page;}
  void ShowPage(int index){if(index<0||index>=pages.Length||index==currentPage)return;pageHost.SuspendLayout();try{pages[index].BringToFront();pages[index].Visible=true;for(int i=0;i<pages.Length;i++){if(i!=index)pages[i].Visible=false;bool selected=i==index;if(tabs[i].Selected!=selected){tabs[i].Selected=selected;tabs[i].Invalidate();}}currentPage=index;}finally{pageHost.ResumeLayout(true);}}
  static void PrepareControls(Control control){if(control.IsDisposed)return;IntPtr handle=control.Handle;foreach(Control child in control.Controls)PrepareControls(child);}
  void InitializeUniformLayout(){if(uniformLayout!=null)return;int selected=currentPage;for(int i=0;i<pages.Length;i++){ShowPage(i);PrepareControls(pages[i]);pages[i].PerformLayout();}ShowPage(selected);freezingLayout=true;try{uniformLayout=new UniformConsoleLayout(this,Controls[0]);AutoScaleMode=AutoScaleMode.None;}finally{freezingLayout=false;}uniformLayout.Apply();}
  void PreparePage(int index){if(IsDisposed||index>=pages.Length)return;PrepareControls(pages[index]);if(index+1<pages.Length)BeginInvoke(new Action(()=>PreparePage(index+1)));}
  void WireInterface(){
   latencyMode.SelectedIndexChanged+=(s,e)=>{settings.BufferMs=latencyMode.SelectedIndex==0?120:180;settings.OutputBufferMs=latencyMode.SelectedIndex==0?60:120;Save();};
   masterVolume.ValueChanged+=(s,e)=>{if(syncingMaster)return;settings.MasterGain=masterVolume.Value/100f;settings.MasterMuted=false;RefreshMasterVolume();Save();PresentVolumeOverlay();};
   masterMute.Click+=(s,e)=>{settings.MasterMuted=!settings.MasterMuted;RefreshMasterVolume();Save();PresentVolumeOverlay();};knobSensitivity.ValueChanged+=(s,e)=>{settings.VolumeStepPercent=(int)knobSensitivity.Value;Save();};
   start.Click+=async(s,e)=>await StartPlayback();stop.Click+=(s,e)=>StopPlayback();calibrate.Click+=async(s,e)=>await AutoCalibrate();test.Click+=async(s,e)=>await TestPlayback();refresh.Click+=(s,e)=>RefreshDevices();
   mode.SelectedIndexChanged+=(s,e)=>{settings.StereoSplit=mode.SelectedIndex==1;Save();};leftGain.ValueChanged+=(s,e)=>{settings.LeftGain=leftGain.Value/100f;leftTrimText.Text=leftGain.Value+"%";Save();};rightGain.ValueChanged+=(s,e)=>{settings.RightGain=rightGain.Value/100f;rightTrimText.Text=rightGain.Value+"%";Save();};
   leftDelay.ValueChanged+=(s,e)=>{if(!busy){settings.LeftDelayMs=(double)leftDelay.Value;settings.Calibrated=false;Save();}};rightDelay.ValueChanged+=(s,e)=>{if(!busy){settings.RightDelayMs=(double)rightDelay.Value;settings.Calibrated=false;Save();}};
   leftDeviceVolume.ValueChanged+=(s,e)=>SetDeviceVolume(true);rightDeviceVolume.ValueChanged+=(s,e)=>SetDeviceVolume(false);left.SelectedIndexChanged+=(s,e)=>RefreshDeviceVolumes();right.SelectedIndexChanged+=(s,e)=>RefreshDeviceVolumes();autoCalibration.CheckedChanged+=(s,e)=>{settings.CalibrateOnStart=autoCalibration.Checked;Save();};
  }
  void UpdateInstruments(){bool active=engine!=null&&engine.Running;leftMeter.Active=rightMeter.Active=active;leftMeter.Peak=active?engine.Left.Peak:0;rightMeter.Peak=active?engine.Right.Peak:0;if(currentPage==0&&WindowState!=FormWindowState.Minimized){leftMeter.Animate();rightMeter.Animate();}playLamp.Active=active;playLamp.Warning=busy;playLamp.Text=busy?"正在处理 · PLEASE WAIT":active?"播放中 · 2 OUTPUTS":"待机 · STANDBY";protectionLamp.Active=active&&engine.SpeakerMuted;protectionLamp.Warning=active&&!engine.SpeakerMuted;protectionLamp.Text=active?(engine.SpeakerMuted?"笔记本扬声器 · 已静音":"笔记本静音保护 · 检查中"):"笔记本静音保护 · 待机";}
 }
}
