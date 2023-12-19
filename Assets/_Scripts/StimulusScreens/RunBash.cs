//Created By Chris 15/09/2021
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace TetheredFlight
{
    //This script processes the bash commands required to move each display to its proper position.
    public class RunBash : MonoBehaviour
    {
        //Working Bash command
        //output = ExecuteBashCommand("t=$(wmctrl -lp | grep $(xprop -root | grep _NET_ACTIVE_WINDOW | head -1 | awk '{print $5}' | sed 's/,//' | sed 's/^0x/0x0/') | cut -c 1-9); echo \"$t\";");
        public string output = "null";
        public string bashcommand = "";
        // Start is called before the first frame update
        
        void Awake()
        {
            // bashcommand = "t=$(echo xdotool key --windowid " + output +  " super+shift+Right);  echo \"$t\";";
        }

        void Start()
        {
            //output = ExecuteBashCommand("t=$ wmctrl -lp | grep $(xprop -root | grep _NET_ACTIVE_WINDOW | head -1 | awk '{print $5}' | sed 's/,//' | sed 's/^0x/0x0/')");
            //output = ExecuteBashCommand("t=$(wmctrl -lp | grep $(xprop -root | grep _NET_ACTIVE_WINDOW | head -1 | awk '{print $5}' | sed 's/,//' | sed 's/^0x/0x0/')); echo \"$t\";");
            //print(output);
        }

        public static string ExecuteBashCommand(string command)
        {
            // according to: https://stackoverflow.com/a/15262019/637142
            // thanks to this we will pass everything as one command
            command = command.Replace("\"","\"\"");

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \""+ command + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();
            return proc.StandardOutput.ReadToEnd();
        }

        // Update is called once per frame
        void Update()
        {
            output = ExecuteBashCommand("t=$(wmctrl -lp | grep $(xprop -root | grep _NET_ACTIVE_WINDOW | head -1 | awk '{print $5}' | sed 's/,//' | sed 's/^0x/0x0/') | cut -c 1-11); echo \"$t\";");
            bashcommand = "t=$(echo xdotool key --windowid " + output +  " super+shift+Right);  echo \"$t\";";
            // var output2 = RunBash.ExecuteBashCommand("t=$(echo xdotool key --windowid " + output +  " super+shift+Right);  echo \"$t\";");
            // print(output2);
        }
    }
}