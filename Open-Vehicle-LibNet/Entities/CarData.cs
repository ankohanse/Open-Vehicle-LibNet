/*
;    Project:       Open-Vehicle-LibNet
;
;    Changes:
;    1.0    2018-11-01  Initial release
;
;    (C) 2018       Anko Hanse
;
; Permission is hereby granted, free of charge, to any person obtaining a copy
; of this software and associated documentation files (the "Software"), to deal
; in the Software without restriction, including without limitation the rights
; to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
; copies of the Software, and to permit persons to whom the Software is
; furnished to do so, subject to the following conditions:
;
; The above copyright notice and this permission notice shall be included in
; all copies or substantial portions of the Software.
;
; THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
; IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
; FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
; AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
; LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
; OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
; THE SOFTWARE.
*/

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenVehicle.LibNet.Entities
{
    // Note: we use Fody to inject INotifyPropertyChanged code into all property setters
    //
    public class CarData : INotifyPropertyChanged
    {

        #region Enums

        public enum DataStale
        {
            Unknown,
            Stale,
            Good        
        }

        #endregion Enums


        #region RAW and cleaned-up values from the OVMS server
        #pragma warning disable IDE1006 // Naming Styles

        //
        // OVMS Server version (message "f")
        //
        public string       server_firmware                     { get; private set; } = "";

        //
        // OVMS Server number of connected cars (message "Z")
        //
        public int          server_cars_connected               { get; private set; } = 0;

        //
        // OVMS Server timestamp of last update (message "T")
        //
        public long         server_lastupdated_raw              { get; private set; } = 0;
        public DateTime     server_lastupdated                  => DateTime.Now.AddSeconds( -1 * server_lastupdated_raw );

        //
        // OVMS Server switched to paranoid mode communications
        //
        public bool         server_paranoid                     { get; set; }

        #pragma warning restore IDE1006 // Naming Styles
        #endregion RAW and cleaned-up values from the OVMS server


        #region RAW and cleaned-up values from the vehicle
        #pragma warning disable IDE1006 // Naming Styles

        //
        // Car firmware (message "F")
        //
        public string       car_firmware                        { get; private set; } = "";
        public string       car_vin                             { get; private set; } = "";
        public string       car_type                            { get; private set; } = "";
        public int          car_can_write_raw                   { get; private set; } = 0;
        public bool         car_can_write                       => (car_can_write_raw > 0);
        public DataStale    stale_firmware                      { get; private set; } = DataStale.Unknown;

        public string       gsm_lock                            { get; private set; } = "";
        public int          gsm_signal                          { get; private set; } = 0;
        public int          gsm_dbm                             => GetGsmDbm(gsm_signal);
        public int          gsm_bars                            => GetGsmBars(gsm_signal);

        //
        // Car firmware capabilities (message "V")
        //
        public bool[]       command_support                     { get; private set; } = new bool[256];

        //
        // Car Status (message "S")
        //
        public string       unit_distance_raw                   { get; private set; } = "";
        public string       unit_distance                       => unit_distance_raw.StartsWith("M") ? "mi" : "km"; 
        public string       unit_speed                          => unit_distance_raw.StartsWith("M") ? "mph" : "kph"; 

        public float        bat_cac                             { get; private set; } = 0.0f;
        public float        bat_soh_raw                         { get; private set; } = 0.0f;
        public string       bat_soh                             => $"{bat_soh_raw:0.0}{Preferences.Instance.UnitSpacer}%";
        public float        bat_soc_raw                         { get; private set; } = 0.0f;
        public string       bat_soc                             => $"{bat_soc_raw:0.0}{Preferences.Instance.UnitSpacer}%";
        public float        bat_power_kw                        { get; private set; } = 0.0f;
        public float        bat_voltage                         { get; private set; } = 0.0f;

        public float        bat_range_estimated_raw             { get; private set; } = 0.0f;
        public string       bat_range_estimated                 { get { return string.Format("{0:0.0}{1}{2}", bat_range_estimated_raw, Preferences.Instance.UnitSpacer, unit_distance); } }
        public float        bat_range_ideal_raw                 { get; private set; } = 0.0f;
        public string       bat_range_ideal                     { get { return string.Format("{0:0.0}{1}{2}", bat_range_ideal_raw, Preferences.Instance.UnitSpacer, unit_distance); } }
        public float        bat_range_full_raw                  { get; private set; } = 0.0f;
        public string       bat_range_full                      { get { return string.Format("{0}{1}{2}", bat_range_full_raw, Preferences.Instance.UnitSpacer, unit_distance); } }

        public float        charge_voltage_raw                  { get; private set; } = 0.0f;
        public string       charge_voltage                      { get { return string.Format("{0:0.0}{1}V", charge_voltage_raw, Preferences.Instance.UnitSpacer); } }
        public float        charge_current_raw                  { get; private set; } = 0.0f;
        public string       charge_current                      { get { return string.Format("{0:0.0}{1}A", charge_current_raw, Preferences.Instance.UnitSpacer); } }
        public string       charge_voltagecurrent               { get { return string.Format("{0:0.0}{2}V {1:0.0}{2}A", charge_voltage_raw, charge_current_raw, Preferences.Instance.UnitSpacer); } }
        public float        charge_currentlimit_raw             { get; private set; } = 0.0f;
        public string       charge_currentimit                  { get { return string.Format("{0:0.0}{1}A", charge_currentlimit_raw, Preferences.Instance.UnitSpacer); } }
        public int          charge_state_i_raw                  { get; private set; } = 0;
        public string       charge_state_s_raw                  { get; private set; } = "";
        public string       charge_state                        { get { return charge_state_s_raw; } }
        public int          charge_substate                     { get; private set; } = 0;
        public int          charge_mode_i_raw                   { get; private set; } = 0;
        public string       charge_mode_s_raw                   { get; private set; } = "";
        public string       charge_mode                         { get; private set; } = "";
        public int          charge_plugtype                     { get; private set; } = 0;
        public int          charge_duration                     { get; private set; } = 0;
        public int          charge_estimate                     { get; private set; } = 0;
        public int          charge_b4                           { get; private set; } = 0;
        public int          charge_kwhconsumed                  { get; private set; } = 0;

        public int          charge_timermode_raw                { get; private set; } = 0;
        public bool         charge_timer                        { get { return (charge_timermode_raw > 0); } }
        public int          charge_timerstart_raw               { get; private set; } = 0;
        public string       charge_time                         { get; private set; } = "";

        public int          charge_full_minsremaining           { get; private set; } = 0;
        public int          charge_limit_minsremaining          { get; private set; } = 0;
        public int          charge_limit_minsremaining_range    { get; private set; } = 0;
        public int          charge_limit_minsremaining_soc      { get; private set; } = 0;
        public int          charge_limit_soclimit               { get; private set; } = 0;
        public float        charge_limit_rangelimit_raw         { get; private set; } = 0.0f;
        public string       charge_limit_rangelimit             { get { return string.Format("{0:0}{1}{2}", charge_limit_rangelimit_raw, Preferences.Instance.UnitSpacer, unit_distance); } }

        public int          cooldown_cooling                    { get; private set; } = 0;
        public int          cooldown_tbattery                   { get; private set; } = 0;
        public int          cooldown_timelimit                  { get; private set; } = 0;

        public int          stale_chargetimer_raw               { get; private set; } = 0;
        public DataStale    stale_chargetimer                   { get { return GetDataStale(stale_chargetimer_raw); } }
        public DataStale    stale_status                        { get; private set; } = DataStale.Good;

        //
        // Car Location (message "L")
        //
        public double       pos_latitude                        { get; private set; } = 0.0;
        public double       pos_longitude                       { get; private set; } = 0.0;
        public double       pos_direction                       { get; private set; } = 0.0;
        public double       pos_altitude                        { get; private set; } = 0.0;
        public int          pos_gpslock_raw                     { get; private set; } = 0;
        public bool         pos_gpslock                         { get { return (pos_gpslock_raw > 0); } }
        public float        pos_gpsspeed_raw                    { get; private set; } = 0.0f;
        public string       pos_gpsspeed                        { get { return string.Format("{0:0.#}{1}{2}", pos_gpsspeed_raw, Preferences.Instance.UnitSpacer, unit_speed); } }

        public int          drive_mode                          { get; private set; } = 0;
        public float        drive_power                         { get; private set; } = 0.0f;
        public float        drive_energy_used                   { get; private set; } = 0.0f;
        public float        drive_energy_recovered              { get; private set; } = 0.0f;

        public int          stale_pos_raw                       { get; private set; } = 0;
        public DataStale    stale_pos                           { get { return GetDataStale(stale_pos_raw); } }

        //
        // Doors / Switches & environment (message "D")
        //
        public int          env_flags1_raw                      { get; private set; } = 0;
        public bool         env_started                         { get { return GetBitField(env_flags1_raw, 0x80); } }
        public bool         env_handbrake_on                    { get { return GetBitField(env_flags1_raw, 0x40); } }
        public bool         env_charging                        { get { return GetBitField(env_flags1_raw, 0x10); } }
        public bool         env_pilot_present                   { get { return GetBitField(env_flags1_raw, 0x08); } }
        public bool         door_chargeport_open                { get { return GetBitField(env_flags1_raw, 0x04); } }
        public bool         door_frontright_open                { get { return GetBitField(env_flags1_raw, 0x02); } }
        public bool         door_frontleft_open                 { get { return GetBitField(env_flags1_raw, 0x01); } }

        public int          env_flags2_raw                      { get; private set; } = 0;
        public bool         door_trunk_open                     { get { return GetBitField(env_flags2_raw, 0x80); } }
        public bool         door_bonnet_open                    { get { return GetBitField(env_flags2_raw, 0x40); } }
        public bool         env_headlights_on                   { get { return GetBitField(env_flags2_raw, 0x20); } }
        public bool         env_valetmode                       { get { return GetBitField(env_flags2_raw, 0x10); } }
        public bool         env_locked                          { get { return GetBitField(env_flags2_raw, 0x08); } }

        public int          env_flags3_raw                      { get; private set; } = 0;
        public bool         env_awake                           { get { return GetBitField(env_flags3_raw, 0x02); } }

        public int          env_flags4_raw                      { get; private set; } = 0;
        public bool         env_alarm_sounding                  { get { return GetBitField(env_flags4_raw, 0x02); } }

        public int          env_flags5_raw                      { get; private set; } = 0;
        public bool         bat_12v_charging                    { get { return GetBitField(env_flags5_raw, 0x10); } }
        public bool         door_rearright_open                 { get { return GetBitField(env_flags5_raw, 0x02); } }
        public bool         door_rearleft_open                  { get { return GetBitField(env_flags5_raw, 0x01); } }

        public int          env_lockstate_raw                   { get; private set; } = 0;
        public int          env_parkedtime_raw                  { get; private set; } = 0;
        public DateTime     env_parkedtime                      { get { return DateTime.Now.AddSeconds( -1 * env_parkedtime_raw ); } }

        public DataStale    stale_environment                   { get; private set; } = DataStale.Unknown;

        public float        temp_pem_raw                        { get; private set; } = 0;
        public string       temp_pem                            { get { return ConvertTemperatureUnit(temp_pem_raw); } }
        public int          temp_motor_raw                      { get; private set; } = 0;
        public string       temp_motor                          { get { return ConvertTemperatureUnit(temp_motor_raw); } }
        public int          temp_battery_raw                    { get; private set; } = 0;
        public string       temp_battery                        { get { return ConvertTemperatureUnit(temp_battery_raw); } }
        public float        temp_charger_raw                    { get; private set; } = 0.0f;
        public string       temp_charger                        { get { return ConvertTemperatureUnit(temp_charger_raw); } }
        public float        temp_cabin_raw                      { get; private set; } = 0;
        public string       temp_cabin                          { get { return ConvertTemperatureUnit(temp_cabin_raw); } }

        public int          stale_temps_raw                     { get; private set; } = 0;
        public DataStale    stale_temps                         { get { return GetDataStale(stale_temps_raw); } }

        public float        temp_ambient_raw                    { get; private set; } = 0.0f;
        public string       temp_ambient                        { get { return ConvertTemperatureUnit(temp_ambient_raw); } }

        public int          stale_temp_ambient_raw              { get; private set; } = 0;
        public DataStale    stale_temp_ambient                  { get { return GetDataStale(stale_temp_ambient_raw); } }

        public float        pos_speed_raw                       { get; private set; } = 0.0f;
        public string       pos_speed                           { get { return string.Format("{0:0.#}{1}{2}", pos_speed_raw, Preferences.Instance.UnitSpacer, unit_speed); } }
        public float        pos_tripmeter_raw                   { get; private set; } = 0.0f;
        public string       pos_tripmeter                       { get { return string.Format("{0:0.#}{1}{2}", pos_tripmeter_raw, Preferences.Instance.UnitSpacer, unit_distance); } }
        public float        pos_odometer_raw                    { get; private set; } = 0.0f;
        public string       pos_odometer                        { get { return string.Format("{0:0}{1}{2}", pos_odometer_raw, Preferences.Instance.UnitSpacer, unit_distance); } }
                                                   
        public float        bat_12v_voltage_raw                 { get; private set; } = 0.0f;
        public string       bat_12v_voltage                     => $"{bat_12v_voltage_raw:0.0}{Preferences.Instance.UnitSpacer}V";
        public float        bat_12v_voltage_ref_raw             { get; private set; } = 0.0f;
        public string       bat_12v_voltage_ref                 => $"{bat_12v_voltage_ref_raw:0.0}{Preferences.Instance.UnitSpacer}V";
        public float        bat_12v_current_raw                 { get; private set; } = 0.0f;
        public string       bat_12v_current                     => $"{bat_12v_current_raw:0.0}{Preferences.Instance.UnitSpacer}A";

        //
        // Tire Pressure (message "W")
        //
        public float        tpms_fr_pressure_raw                { get; private set; } = 0.0f;
        public string       tpms_fr_pressure                    { get { return string.Format("{0:#.0}{1}psi", tpms_fr_pressure_raw, Preferences.Instance.UnitSpacer); } }
        public float        tpms_rr_pressure_raw                { get; private set; } = 0.0f;
        public string       tpms_rr_pressure                    { get { return string.Format("{0:#.0}{1}psi", tpms_rr_pressure_raw, Preferences.Instance.UnitSpacer); } }
        public float        tpms_fl_pressure_raw                { get; private set; } = 0.0f;
        public string       tpms_fl_pressure                    { get { return string.Format("{0:#.0}{1}psi", tpms_fl_pressure_raw, Preferences.Instance.UnitSpacer); } }
        public float        tpms_rl_pressure_raw                { get; private set; } = 0.0f;
        public string       tpms_rl_pressure                    { get { return string.Format("{0:#.0}{1}psi", tpms_rl_pressure_raw, Preferences.Instance.UnitSpacer); } }

        public float        tpms_fr_temp_raw                    { get; private set; } = 0.0f;
        public string       tpms_fr_temp                        { get { return ConvertTemperatureUnit(tpms_fr_temp_raw); } }
        public float        tpms_rr_temp_raw                    { get; private set; } = 0.0f;
        public string       tpms_rr_temp                        { get { return ConvertTemperatureUnit(tpms_rr_temp_raw); } }
        public float        tpms_fl_temp_raw                    { get; private set; } = 0.0f;
        public string       tpms_fl_temp                        { get { return ConvertTemperatureUnit(tpms_fl_temp_raw); } }
        public float        tpms_rl_temp_raw                    { get; private set; } = 0.0f;
        public string       tpms_rl_temp                        { get { return ConvertTemperatureUnit(tpms_rl_temp_raw); } }

        public int          stale_tpms_raw                      { get; private set; } = 0;
        public DataStale    stale_tpms                          { get { return GetDataStale(stale_tpms_raw); } }

        //
        // Other
        //

        // Renault Twizy specific
    	public int         rt_cfg_type              { get { return (car_type=="RT") ? (drive_mode & 0x01)      : 0; } }		// CFG: 0=Twizy80, 1=Twizy45
    	public int         rt_cfg_profile_user      { get { return (car_type=="RT") ? (drive_mode & 0x06) >> 1 : 0; } }		// CFG: user selected profile: 0=Default, 1..3=Custom
    	public int         rt_cfg_profile_cfgmode   { get { return (car_type=="RT") ? (drive_mode & 0x18) >> 3 : 0; } }		// CFG: profile, cfgmode params were last loaded from
    	public int         rt_cfg_unsaved           { get { return (car_type=="RT") ? (drive_mode & 0x20) >> 5 : 0; } } 	// CFG: RAM profile changed & not yet saved to EEPROM
    	public int         rt_cfg_applied           { get { return (car_type=="RT") ? (drive_mode & 0x80) >> 7 : 0; } }		// CFG: applyprofile success flag

        #pragma warning restore IDE1006 // Naming Styles
        #endregion RAW and cleaned-up values from the vehicle


        #region construction

        public CarData()
        {
        }

        #endregion construction

        
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged


        #region Property formatting methods

        private DataStale GetDataStale(int raw)
        {
            if (raw < 0)
                return DataStale.Unknown;
            
            else if (raw > 0) 
                return DataStale.Good;
            
            else
                return DataStale.Stale;
        }


        private bool GetBitField(int value, int mask)
        {
            return ((value & mask) == mask);
        }

        
        private string ConvertDistanceUnit(int distance)
        {
            if (string.IsNullOrEmpty(unit_distance))
                return "";

            else if (unit_distance == "M")
                return string.Format("{0}{1}mi", distance, Preferences.Instance.UnitSpacer);

            else
                return string.Format("{0}{1}km", distance, Preferences.Instance.UnitSpacer);
        }


        private string ConvertTemperatureUnit(float temperature)
        {
            string unit = "C";

            if (Preferences.Instance.UnitForTemperature == Preferences.UnitTemperature.Fahrenheit)
            {
                temperature = (temperature * 9.0f / 5.0f) + 32.0f;
                unit = "F";
            }

            return string.Format("{0:#.0}{1}{2}{3}", temperature, Preferences.Instance.UnitSpacer, '\u00B0', unit);
        }


        private int GetGsmDbm(int level)
        {
            if (level <= 31)
                return -113 + 2 * level;
             else
                return 0;
        }

        private int GetGsmBars(int level)
        {
            int dbm = GetGsmDbm(level);

            if (dbm < -121 || dbm >= 0)
                return 0;
            else if (dbm < -107)
                return 1;
            else if (dbm < -98)
                return 2;
            else if (dbm < -87)
                return 3;
            else if (dbm < -76)
                return 4;
            else 
                return 5;
        }

        #endregion Property Formatting methods


        #region Extract values from OVMS messages

        //
        // OVMS Server connect/disconnect
        //
        internal void ProcessResetServer()
        {
            server_lastupdated_raw      = 0;
            server_cars_connected   = 0;

            server_paranoid             = false;
        }


        //
        // OVMS Server version (message "f")
        //
        internal void ProcessMsgServer(OVMSMessage msg)
        {
            if (msg.Params.Count > 0)
            {
                server_firmware             = msg.Params[0];
            }
        }


        //
        // OVMS Server number of connected cars (message "Z")
        //
        internal void ProcessMsgConnectedCars(OVMSMessage msg)
        {
            if (msg.Params.Count > 0)
            {
                server_cars_connected       = int.Parse(msg.Params[0]);
            }
        }


        //
        // OVMS Server timestamp of last update (message "T")
        //
        internal void ProcessMsgTimestamp(OVMSMessage msg)
        {
            if (msg.Params.Count > 0)
            {
                server_lastupdated_raw      = long.Parse(msg.Params[0]);
            }
        }


        //
        // Car Firmware (message "F")
        //
        internal void ProcessMsgFirmware(OVMSMessage msg)
        {
            if (msg.Params.Count > 2)
            {
                car_firmware                = msg.Params[0];
                car_vin                     = msg.Params[1];
                gsm_signal                  = int.Parse(msg.Params[2]);

                stale_firmware              = DataStale.Good;
            }
            if (msg.Params.Count > 4)
            {
                car_can_write_raw           = int.Parse(msg.Params[3]);
                car_type                    = msg.Params[4];
            }
            if (msg.Params.Count > 5)
            {
                gsm_lock                    = msg.Params[5];
            }
        }


        //
        // Car firmware capabilities (message "V")
        //
        internal void ProcessMsgCapabilities(OVMSMessage msg)
        {
            // msgdata format: C1,C3-6,...
		    // translate to bool array
            command_support = new bool[256];

            foreach (string part in msg.Params)
            {
                if (part.StartsWith("C"))
                {
                    string[] caps = part.Substring(1).Split( new char[] { '-' } );
                    int start = int.Parse(caps[0]);
                    int end   = (caps.Length > 1) ? int.Parse(caps[1]) : start;

                    for (int i = start; i <= end; i++)
                    {
                        command_support[i] = true;
                    }
                }
            }
        }


        //
        // Car Status (message "S")
        //
        internal void ProcessMsgStatus(OVMSMessage msg)
        {
            if (msg.Params.Count > 7)
            {
                bat_soc_raw                         = float.Parse(msg.Params[0]);
                unit_distance_raw                   = msg.Params[1];
                charge_voltage_raw                  = float.Parse(msg.Params[2]);
                charge_current_raw                  = float.Parse(msg.Params[3]);
                charge_state_s_raw                  = msg.Params[4];
                charge_mode_s_raw                   = msg.Params[5];
                bat_range_ideal_raw                 = float.Parse(msg.Params[6]);
                bat_range_estimated_raw             = float.Parse(msg.Params[7]);
                stale_status                        = DataStale.Good;
            }
            if (msg.Params.Count > 14)
            {
                charge_currentlimit_raw             = float.Parse(msg.Params[8]);
                charge_duration                     = int.Parse(msg.Params[9]);
                charge_b4                           = int.Parse(msg.Params[10]);
                charge_kwhconsumed                  = int.Parse(msg.Params[11]);
                charge_substate                     = int.Parse(msg.Params[12]);
                charge_state_i_raw                  = int.Parse(msg.Params[13]);
                charge_mode_i_raw                   = int.Parse(msg.Params[14]);
            }
            if (msg.Params.Count > 17)
            {
                charge_timermode_raw                = int.Parse(msg.Params[15]);
                charge_timerstart_raw               = int.Parse(msg.Params[16]);
                stale_chargetimer_raw               = int.Parse(msg.Params[17]);
            }
            if (msg.Params.Count > 18)
            {
                bat_cac                             = float.Parse(msg.Params[18]);
            }
            if (msg.Params.Count > 26)
            {
                charge_full_minsremaining           = int.Parse(msg.Params[19]);
                charge_limit_minsremaining          = int.Parse(msg.Params[20]);
                charge_limit_rangelimit_raw         = float.Parse(msg.Params[21]);
                charge_limit_soclimit               = int.Parse(msg.Params[22]);
                cooldown_cooling                    = int.Parse(msg.Params[23]);
                cooldown_tbattery                   = int.Parse(msg.Params[24]);
                cooldown_timelimit                  = int.Parse(msg.Params[25]);
                charge_estimate                     = int.Parse(msg.Params[26]);
            }
            if (msg.Params.Count > 29)
            {
                charge_limit_minsremaining_range    = int.Parse(msg.Params[27]);    
                charge_limit_minsremaining_soc      = int.Parse(msg.Params[28]);
                bat_range_full_raw                  = int.Parse(msg.Params[29]);
            }
            if (msg.Params.Count > 32)
            {
                charge_plugtype                     = int.Parse(msg.Params[30]);
                bat_power_kw                        = float.Parse(msg.Params[31]);
                bat_voltage                         = float.Parse(msg.Params[32]);
            }
            if (msg.Params.Count > 33)
            {
                bat_soh_raw                         = float.Parse(msg.Params[33]);
            }
        }


        //
        // Car Location (message "L")
        //
        internal void ProcessMsgLocation(OVMSMessage msg)
        {
            if (msg.Params.Count > 1)
            {
                pos_latitude                        = double.Parse(msg.Params[0]);
                pos_longitude                       = double.Parse(msg.Params[1]);
            }
            if (msg.Params.Count > 5)
            {
                pos_direction                       = double.Parse(msg.Params[2]);
                pos_altitude                        = double.Parse(msg.Params[3]);
                pos_gpslock_raw                     = int.Parse(msg.Params[4]);
                stale_pos_raw                       = int.Parse(msg.Params[5]);
            }
            if (msg.Params.Count > 7)
            {
                pos_gpsspeed_raw                    = float.Parse(msg.Params[6]);
                pos_tripmeter_raw                   = float.Parse(msg.Params[7]);
            }
            if (msg.Params.Count > 11)
            {
                drive_mode                          = int.Parse(msg.Params[8]);
                drive_power                         = float.Parse(msg.Params[9]);
                drive_energy_used                   = float.Parse(msg.Params[10]);
                drive_energy_recovered              = float.Parse(msg.Params[11]);
            }
        }
         

        //
        // Doors / Switches & environment (message "D")
        //
        internal void ProcessMsgDoors(OVMSMessage msg)
        {
            if (msg.Params.Count > 8)
            {
                env_flags1_raw                      = int.Parse(msg.Params[0]);
                env_flags2_raw                      = int.Parse(msg.Params[1]);
                env_lockstate_raw                   = int.Parse(msg.Params[2]);
                temp_pem_raw                        = float.Parse(msg.Params[3]);
                temp_motor_raw                      = int.Parse(msg.Params[4]);
                temp_battery_raw                    = int.Parse(msg.Params[5]);
                pos_tripmeter_raw                   = float.Parse(msg.Params[6]);
                pos_odometer_raw                    = float.Parse(msg.Params[7]);
                pos_speed_raw                       = float.Parse(msg.Params[8]);

                stale_environment                   = DataStale.Good;
            }
            if (msg.Params.Count > 13)
            {
                env_parkedtime_raw                  = int.Parse(msg.Params[9]);
                temp_ambient_raw                    = float.Parse(msg.Params[10]);
                env_flags3_raw                      = int.Parse(msg.Params[11]);
                stale_temps_raw                     = int.Parse(msg.Params[12]);
                stale_temp_ambient_raw              = int.Parse(msg.Params[13]);
            }
            if (msg.Params.Count > 15)
            {
                bat_12v_voltage_raw                 = float.Parse(msg.Params[14]);
                env_flags4_raw                      = int.Parse(msg.Params[15]);
            }
            if (msg.Params.Count > 17)
            {
                bat_12v_voltage_ref_raw             = float.Parse(msg.Params[16]);
                env_flags5_raw                      = int.Parse(msg.Params[17]);
            }
            if (msg.Params.Count > 18)
            {
                temp_charger_raw                    = float.Parse(msg.Params[18]);
            }
            if (msg.Params.Count > 19)
            {
                bat_12v_current_raw                 = float.Parse(msg.Params[19]);
            }
            if (msg.Params.Count > 20)
            {
                temp_cabin_raw                      = float.Parse(msg.Params[20]);
            }
        }
         

        //
        // Tire Pressure (message "W")
        //
        internal void ProcessMsgTirePressure(OVMSMessage msg)
        {
            if (msg.Params.Count > 8)
            {
                tpms_fr_pressure_raw                = float.Parse(msg.Params[0]);
                tpms_fr_temp_raw                    = float.Parse(msg.Params[1]);
                tpms_rr_pressure_raw                = float.Parse(msg.Params[2]);
                tpms_rr_temp_raw                    = float.Parse(msg.Params[3]);
                tpms_fl_pressure_raw                = float.Parse(msg.Params[4]);
                tpms_fl_temp_raw                    = float.Parse(msg.Params[5]);
                tpms_rl_pressure_raw                = float.Parse(msg.Params[6]);
                tpms_rl_temp_raw                    = float.Parse(msg.Params[7]);
                stale_tpms_raw                      = int.Parse(msg.Params[8]);
            }
        }

        #endregion Extract values from OVMS messages

    }


}
