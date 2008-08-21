using System;
using System.Collections.Generic;
using System.Text;
using VVVV.PluginInterfaces.V1;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using BassSound.Internals;
using System.IO;

namespace BassSound.Streams
{
    public class BassFileStreamNode : IPlugin, IDisposable
    {
        #region Plugin Information
        public static IPluginInfo PluginInfo
        {
            get
            {
                IPluginInfo Info = new PluginInfo();
                Info.Name = "FileStream";
                Info.Category = "Bass";
                Info.Version = "";
                Info.Help = "Bass API WDM File Stream Node";
                Info.Bugs = "";
                Info.Credits = "";
                Info.Warnings = "";

                //leave below as is
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
                System.Diagnostics.StackFrame sf = st.GetFrame(0);
                System.Reflection.MethodBase method = sf.GetMethod();
                Info.Namespace = method.DeclaringType.Namespace;
                Info.Class = method.DeclaringType.Name;
                return Info;
            }
        }
        #endregion

        private IPluginHost FHost;

        private IValueIn FPinCfgIsDecoding;

        private IValueIn FPinInPlay;
        private IStringIn FPinInFilename;
        private IValueIn FPInInLoop;
        private IValueIn FPinInDoSeek;
        private IValueIn FPinInPosition;
        private IValueIn FPinInMono;
        private IValueIn FPinInPitch;
        private IValueIn FPinInTempo;

        private IValueOut FPinOutHandle;
        private IValueOut FPinOutCurrentPosition;
        private IValueOut FPinOutLength;

        private bool FConnected = false;

        private int FHandle = 0;

        #region Set Plugin Host
        public void SetPluginHost(IPluginHost Host)
        {
            this.FHost = Host;

            //Config Pins
            this.FHost.CreateValueInput("Is Decoding", 1, null, TSliceMode.Single, TPinVisibility.OnlyInspector, out this.FPinCfgIsDecoding);
            this.FPinCfgIsDecoding.SetSubType(0, 1, 1, 0, false, true, true);

            //Input Pins
            this.FHost.CreateValueInput("Play", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinInPlay);
            this.FPinInPlay.SetSubType(0, 1, 1, 0, false, true, true);

            this.FHost.CreateValueInput("Loop", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPInInLoop);
            this.FPInInLoop.SetSubType(0, 1, 1, 0, false, true, true);

            this.FHost.CreateValueInput("Do Seek", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinInDoSeek);
            this.FPinInDoSeek.SetSubType(0, 1, 0, 0, true, false, true);

            this.FHost.CreateValueInput("Seek Position", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinInPosition);
            this.FPinInPosition.SetSubType(0, double.MaxValue, 0, 0.0, false, false, false);

            this.FHost.CreateValueInput("Mono", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinInMono);
            this.FPinInMono.SetSubType(0, 1, 1, 0, false, true, true);

            this.FHost.CreateValueInput("Pitch", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinInPitch);
            this.FPinInPitch.SetSubType(-60, 60, 0, 0, false, false, false);

            this.FHost.CreateValueInput("Tempo", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinInTempo);
            this.FPinInTempo.SetSubType(-95, 5000, 0, 0, false, false, false);

            this.FHost.CreateStringInput("Filename", TSliceMode.Single, TPinVisibility.True, out this.FPinInFilename);
            this.FPinInFilename.SetSubType("", true);

            //Output Pins
            this.FHost.CreateValueOutput("Handle", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinOutHandle);
            this.FPinOutHandle.SetSubType(double.MinValue, double.MaxValue, 0, 0, false, false, true);

            this.FHost.CreateValueOutput("CurrentPosition", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinOutCurrentPosition);
            this.FPinOutCurrentPosition.SetSubType(0, double.MaxValue, 0, 0.0, false, false, false);

            this.FHost.CreateValueOutput("Length", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinOutLength);
            this.FPinOutLength.SetSubType(0, double.MaxValue, 0, 0.0, false, false, false);
        }
        #endregion

        #region Configurate
        public void Configurate(IPluginConfig Input)
        {

        }
        #endregion

        #region Evaluate
        public void Evaluate(int SpreadMax)
        {
            bool updateplay = false;
            bool updateloop = false;

            if (this.FConnected != this.FPinOutHandle.IsConnected)
            {
                updateplay = true;
                this.FConnected = this.FPinOutHandle.IsConnected;
            }

            #region Reset pins
            if (this.FPinInFilename.PinIsChanged || this.FPinCfgIsDecoding.PinIsChanged || this.FPinInMono.PinIsChanged)
            {
                string file;
                this.FPinInFilename.GetString(0, out file);

                if (File.Exists(file))
                {
                    FileChannelInfo info = new FileChannelInfo();
                    info.FileName = file;

                    double isdecoding;
                    this.FPinCfgIsDecoding.GetValue(0, out isdecoding);
                    info.IsDecoding = isdecoding ==1;

                    ChannelsManager.CreateChannel(info);
                    this.FHandle = info.InternalHandle;

                    this.FPinOutHandle.SetValue(0, this.FHandle);

                    updateplay = true;
                    updateloop = true;
                }
            }
            #endregion

            #region Update Play/Pause
            if (updateplay || this.FPinInPlay.PinIsChanged)
            {
                if (this.FHandle != 0)
                {
                    double doplay;
                    this.FPinInPlay.GetValue(0, out doplay);
                    if (doplay == 1 && this.FPinOutHandle.IsConnected)
                    {
                        ChannelsManager.GetChannel(this.FHandle).Play = true;
                    }
                    else
                    {
                        ChannelsManager.GetChannel(this.FHandle).Play = false;
                    }
                }
            }
            #endregion

            #region Update Looping
            if (updateloop || this.FPInInLoop.PinIsChanged)
            {
                if (this.FHandle != 0)
                {
                    double doloop;
                    this.FPInInLoop.GetValue(0, out doloop);
                    if (doloop == 1)
                    {
                        ChannelsManager.GetChannel(this.FHandle).Loop = true;
                    }
                    else
                    {
                        ChannelsManager.GetChannel(this.FHandle).Loop = false;
                    }
                }
            }
            #endregion

            #region Update Seek position
            if (this.FPinInDoSeek.PinIsChanged && this.FHandle != 0)
            {
                double doseek;
                this.FPinInDoSeek.GetValue(0, out doseek);
                if (doseek == 1)
                {
                    ChannelInfo info = ChannelsManager.GetChannel(this.FHandle);
                    if (info.BassHandle.HasValue)
                    {
                        double position;
                        this.FPinInPosition.GetValue(0, out position);
                        Bass.BASS_ChannelSetPosition(info.BassHandle.Value, (float)position);
                    }
                }
            }
            #endregion

            #region Update Current Position/Length
            if (this.FHandle != 0)
            {
                ChannelInfo info = ChannelsManager.GetChannel(this.FHandle);
                if (info.BassHandle.HasValue)
                {
                    long pos = Bass.BASS_ChannelGetPosition(info.BassHandle.Value);
                    double seconds = Bass.BASS_ChannelBytes2Seconds(info.BassHandle.Value, pos);
                    this.FPinOutCurrentPosition.SetValue(0, seconds);
                    this.FPinOutLength.SetValue(0, info.Length);
                }
            }
            #endregion

            #region Tempo and Pitch
            if (this.FPinInPitch.PinIsChanged || this.FPinInTempo.PinIsChanged)
            {
                if (this.FHandle != 0)
                {
                    double pitch, tempo;
                    this.FPinInPitch.GetValue(0, out pitch);
                    this.FPinInTempo.GetValue(0, out tempo);
                    
                    FileChannelInfo info = (FileChannelInfo)ChannelsManager.GetChannel(this.FHandle);
                    info.Pitch = pitch;
                    info.Tempo = tempo;
                }
            }
            #endregion

            /*
            #region File Change
            if (this.FPinInFilename.PinIsChanged || this.FPinCfgIsDecoding.PinIsChanged || this.FPinInMono.PinIsChanged)
            {
                Bass.BASS_SetDevice(0);
                string file;
                this.FPinInFilename.GetString(0, out file);

                Bass.BASS_StreamFree(this.FHandle);
                this.FHandle = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN);

                //Creates a tempo channel
                BASSFlag flags = BASSFlag.BASS_FX_FREESOURCE | BASSFlag.BASS_SAMPLE_FLOAT;
                
                
                //Decoding channel?
                double isdecoding;
                this.FPinCfgIsDecoding.GetValue(0, out isdecoding);
                if (isdecoding == 1)
                {
                    flags = flags | BASSFlag.BASS_STREAM_DECODE;
                }

                //Mono Channel?
                double mono;
                this.FPinInMono.GetValue(0, out mono);
                if (mono == 1)
                {
                    flags = flags | BASSFlag.BASS_SAMPLE_MONO;
                }
                
                this.FHandle = BassFx.BASS_FX_TempoCreate(this.FHandle, flags);
             
                this.FPinOutHandle.SetValue(0, this.FHandle);
                long len = Bass.BASS_ChannelGetLength(this.FHandle);
                this.FPinOutLength.SetValue(0, Bass.BASS_ChannelBytes2Seconds(this.FHandle, len));

                updateplay = true;
                updateloop = true;
            }
            #endregion

             */
        }
        #endregion

        #region Auto Evaluate
        public bool AutoEvaluate
        {
            get { return false; }
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            Bass.BASS_ChannelStop(this.FHandle);
            Bass.BASS_StreamFree(this.FHandle);
        }
        #endregion
    }
}
