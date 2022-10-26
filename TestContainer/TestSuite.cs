﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Consola.Test;
using Stepflow;
using Stepflow.Gui;
using Stepflow.Midi;
using Stepflow.Gui.Automation;
using Stepflow.Gui.Geometry;
using System.Windows.Forms;
using System.Drawing;
using Win32Imports.Midi;
using static Consola.Test.ConTrol;
using static System.Threading.Thread;
using Button = Consola.Test.ConTrol.Button;
using Point = Consola.Test.ConTrol.Point;

namespace MidiGUI.Test
{
    public class SuiteGUIControls
        : Suite<Container.Form1>
        , IMidiControlElement<MidiInOut>
    {
        private MidiInOut         midiInOut;
        private Container.JogDial jogdial;
        private Container.Sliders sliders = null;
        private Container.Meters  meters = null;
        private Win32Imports.Midi.Message ExpectedMidi = new Win32Imports.Midi.Message(0u);
        internal Container.Form1 GetAut() { return Aut; }

        public SuiteGUIControls( Container.Form1 mainwindow, TestResults args )
            : this( mainwindow, args, string.Empty )
        {}

        public SuiteGUIControls( Container.Form1 mainwindow, TestResults args, string testcase )
            : base( mainwindow
                  , args.HasFlag(TestResults.Verbose)
                  , args.HasFlag(TestResults.XmlOutput)
                  ) {

            switch( testcase ) {
                case "LedButton": AddTestCase( testcase, Test_LedButton ); break;
                case "GuiMeter":  AddTestCase( testcase, Test_GuiMeter ); break;
                case "GuiSlider": AddTestCase( testcase, Test_GuiSlider ); break;
                case "JogDial":   AddTestCase( testcase, Test_JogDial ); break;
                default:
                    AddTestCase( "LedButton", Test_LedButton );
                    AddTestCase( "GuiMeter", Test_GuiMeter );
                    AddTestCase( "GuiSlider", Test_GuiSlider );
                    AddTestCase( "JogDial", Test_JogDial );
                break;
            }

            midiInOut = new MidiInOut();
            midi().binding.AutomationEvent += midi().OnIncommingMidiControl;
            
            midipoller = new Task( midiQueueAction );
            midipoller.Start();
        }

        protected override ConTrol.Point GetMenuPoint( string menupath )
        {
            return Aut.GetMenuPosition( menupath ).ConTrolPoint();
        }

        protected override Area GetScreenArea( object descriptor )
        {
            IRectangle rect;
            if( descriptor is Rectangle ) {
                rect = new SystemDefault( (Rectangle)descriptor );
            } else if( descriptor is IRectangle ) {
                rect = descriptor as IRectangle;
            } else if( descriptor is string ) {
                string name = (string)descriptor;
                if( name == "Testling" ) return Aut.GetTestlingArea().ConTrolArea();
                else rect = CenterAndScale.FromRectangle( Aut.Controls.Find( (string)descriptor, false )[0].Bounds );
            } else if( descriptor is Control ) {
                rect = new SystemDefault( (descriptor as Control).Bounds );
            } else rect = CenterAndScale.Empty;
            Area area = new Area( rect.W, rect.H );
            return area.At( new ConTrol.Point( Win.Point.X + rect.X, Win.Point.Y + rect.Y ) );
        }

        public Area StyleButton {
            get { return GetScreenArea("btn_set_Style"); }
        }

        public Area OrientationButton {
            get { return GetScreenArea("btn_set_Orientation"); }
        }

        public Area InvertButton {
            get { return GetScreenArea("btn_Invert"); }
        }

        public Area ColorButton {
            get { return GetScreenArea("btn_set_Led"); }
        }

        protected override Area GetWindowArea()
        {
            return Aut.location.ConTrolArea();
        }

        internal void SelectControlType( Type controltype )
        {
            Click( Button.L, GetMenuPoint( "Controlls" ) );
            Sleep( 2000 );
            ConTrol.Point point = GetMenuPoint( "Controlls." + controltype.Name );
            Click( Button.L, point );
            Sleep( 2000 );
        }


        private void Test_GuiMeter()
        {
            if( meters == null ) {
                meters = new Container.Meters( this, Aut );
            } meters.Test_GuiMeter();
        }


        private void Test_JogDial()
        {
            SelectControlType( typeof(JogDial) );
            jogdial = new Container.JogDial( this, Aut.GetStagedControl() as JogDial );
            CheckStep( jogdial.InstanceExists, "{0} instanciated", CurrentCase );
            Sleep( 1000 );
            jogdial.SwitchLEDColor();
            jogdial.SwitchLEDColor();
            jogdial.TestInteraction();
        }


        private void Test_LedButton()
        {
            SelectControlType( typeof(LedButton) );

            LedButton testling = Aut.GetStagedControl() as LedButton;
            CheckStep( testling != null, "{0} instanciated", CurrentCase );
            Thread.Sleep( 1000 );

            Aut.SetControlValue( LedButton.Default.ON );
            Thread.Sleep( 1000 );
            MatchStep( testling.State.ToString(), "ON", "LedButton.State", "...shows lable as expected" );
            Thread.Sleep( 1000 );

            Point32 point = Aut.GetTestlingArea().Center;
            InfoStep( "Clicking {0} at x:{1},y:{2}", CurrentCase, point.X, point.Y );
            ConTrol.Click( ConTrol.Button.L, point.X, point.Y );
            Thread.Sleep( 1000 );

            MatchStep( testling.Text, "OFF", "LedButton.State", "...state changed as expected" );
            Thread.Sleep( 1000 );
        }

        private void Test_GuiSlider()
        {
            if( sliders == null ) {
                sliders = new Container.Sliders( this, Aut );
            } sliders.Test_GuiSlider();
        }


        private AutomationlayerAddressat[] midisettings = new AutomationlayerAddressat[1];
        public Value MidiValue { get; set; }
        public MidiInOut binding { get { return midiInOut; } }
        public MidiInputMenu<MidiInOut> inputMenu { get; set; }
        public MidiOutputMenu<MidiInOut> outputMenu { get; set; }

        public AutomationlayerAddressat[] channels { get { return midisettings; } }

        private bool midiloop = true;
        private Task midipoller;
        public void midiQueueAction()
        {
            EventArgs e = new EventArgs();
            while( midiloop ) {
                if( midi().binding.automation().messageAvailable() )
                    midi().binding.automation().ProcessMessageQueue( this, e );
                midipoller.Wait( 10 );
            }
        }

        public void BindMidiControl()
        {
            //AutomationlayerAddressat autput = new AutomationlayerAddressat();
            //(Aut.GetStagedControl() as IAutomationController<Win32Imports.Midi.Message>).ConfigureAsMessagingAutomat()
        }

        public void OnIncommingMidiControl( object sender, Win32Imports.Midi.Message value )
        {
            if( value.Value == ExpectedMidi.Value )
                PassStep( "Received expected Midi Message: {0}", value );
            else
                FailStep( "Received nonsense: {0}", value );
        }

        public IMidiControlElement<MidiInOut> midi()
        {
            return this;
        }
    }
}
