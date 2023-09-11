# ShellyExporters
All Shelly Exporters for Prometheus

This is a repository where I keep all my Shelly Exporters and make them available for others that look for them.
Other implementations were quite old and didn't give enough flexibility so I made my own in C#.

The intent is to have each exporter as a docker container since it is technically recommended by Prometheus that
one exporter should export one device.  
To me it looks quite inefficient to spin up more instances just to send a few more HTTP requests.  
But if you wish to do so then there is no problem - just configure each instance for one device.

## Shelly Plug Exporter
DockerHub: https://hub.docker.com/r/lukassoo/shelly-plug-exporter  
Docker Pull Command (Images for x86 and ARM (like a Rapsberry) available):
   
    docker pull lukassoo/shelly-plug-exporter
    docker pull lukassoo/shelly-plug-exporter:armv7
    docker pull lukassoo/shelly-plug-exporter:arm64

Docker Run Command (x86):

    docker run --name ShellyPlugExporter -d -p 9918:9918 -v /etc/shellyPlugExporter:/Config lukassoo/shelly-plug-exporter
    
Docker Run Command (armv7 - 32bit):

    docker run --name ShellyPlugExporter -d -p 9918:9918 -v /etc/shellyPlugExporter:/Config lukassoo/shelly-plug-exporter:armv7

Docker Run Command (arm64 - 64bit):

    docker run --name ShellyPlugExporter -d -p 9918:9918 -v /etc/shellyPlugExporter:/Config lukassoo/shelly-plug-exporter:arm64
    
As the command implies - it will store the config in "/etc/shellyPlugExporter", change it to your liking.  
**The default config will be generated and the container will stop on the first start**

Example Grafana dashboard using Prometheus as a data source:
![image](https://user-images.githubusercontent.com/10761509/204153225-c67c817c-270b-4cf0-999d-8b0eb2b59d17.png)

Dashboard link: https://grafana.com/grafana/dashboards/19506-power-usage/

It should have everything anyone would want - all useful metrics from the device
* Temperature
* Current Power
* Relay state

(Note: The Temperature is only available in the Shelly Plug **S**)

I left out the total power since it resets every time the device restarts so there is not much use of it.  
You are far better off using the current power metric and just calculating the total power used.  
If someone actually has a good use case for the total power - let me know, I will add it.

The configuration file allows us to define multiple target devices with credentials if using authentication (which you should have enabled
if the device runs in your LAN where anyone can access it by IP and just turn it on/off with one click)  
Also there are "ignore" variables that allow you to disable specific metrics since you might not need to know at all times that the relay is constantly on

The default config:

    targets:
    - name: Your Name for the device
      url: Address (usually 192.168.X.X - the IP of your device)
      username: Username (leave empty if not used but you should secure your device from unauthorized access in some way)
      password: Password (leave empty if not used)
      ignorePowerMetric: false
      ignoreTemperatureMetric: false
      ignoreRelayStateMetric: false
     
Example working config:

     targets:
     - name: ServerPlug
       url: 192.168.1.7
       username: SomeUser
       password: VerySecurePassword
       ignorePowerMetric: false
       ignoreTemperatureMetric: false
       ignoreRelayStateMetric: true

Example multi-device config:

     targets:
     - name: ServerPlug
       url: 192.168.1.7
       username: SomeUser
       password: VerySecurePassword
       ignorePowerMetric: false
       ignoreTemperatureMetric: false
       ignoreRelayStateMetric: true
     - name: AnotherOne
       url: 192.168.1.8
       username: SomeUser
       password: VerySecurePassword2?
       ignorePowerMetric: false
       ignoreTemperatureMetric: false
       ignoreRelayStateMetric: true
       
Please make sure you actually change the credentials to the ones you use when you secure your devices

Once running - if you go to the address you will see something like this:

    # HELP shellyplug_ServerPlug_currently_used_power The amount of power currently flowing through the plug in watts
    # TYPE shellyplug_ServerPlug_currently_used_power gauge
    shellyplug_ServerPlug_currently_used_power 262.30
    # HELP shellyplug_ServerPlug_temperature The internal device temperature
    # TYPE shellyplug_ServerPlug_temperature gauge
    shellyplug_ServerPlug_temperature 34.2
This is the format that Prometheus wants

## Shelly 3EM Exporter
DockerHub: https://hub.docker.com/r/lukassoo/shelly-3em-exporter  
Docker Pull Command (Images for x86 and ARM (like a Rapsberry) available):
   
    docker pull lukassoo/shelly-3em-exporter
    docker pull lukassoo/shelly-3em-exporter:armv7
    docker pull lukassoo/shelly-3em-exporter:arm64
    
Docker Run Command:

    docker run --name Shelly3EmExporter -d -p 9946:9946 -v /etc/shelly3EmExporter:/Config lukassoo/shelly-3em-exporter

Docker Run Command (armv7 - 32bit):

    docker run --name Shelly3EmExporter -d -p 9946:9946 -v /etc/shelly3EmExporter:/Config lukassoo/shelly-3em-exporter:armv7

Docker Run Command (arm64 - 64bit):

    docker run --name Shelly3EmExporter -d -p 9946:9946 -v /etc/shelly3EmExporter:/Config lukassoo/shelly-3em-exporter:arm64


As the command implies - it will store the config in "/etc/shelly3EmExporter", change it to your liking.  
**The default config will be generated and the container will stop on the first start**

Example Grafana dashboard using Prometheus as a data source:
![image](https://user-images.githubusercontent.com/10761509/204153510-fabbe4a7-2cea-4ffa-afaf-b48292675117.png)

Dashboard link: https://grafana.com/grafana/dashboards/19500-solar-power/

The configs are a bit longer since the Shelly 3EM has multiple meters, you can choose which ones you want to have metrics from, what you want to know from them (power, voltage, current, power factor) and if you want to know about the relay state:

     targets:
     - name: solar_energy
       url: 192.168.1.7
       username: SomeUser
       password: VerySecurePassword
       targetMeters:
       - index: 0
         ignorePower: false
         ignoreVoltage: false
         ignoreCurrent: false
         ignorePowerFactor: false
       - index: 1
         ignorePower: false
         ignoreVoltage: false
         ignoreCurrent: false
         ignorePowerFactor: false
       - index: 2
         ignorePower: false
         ignoreVoltage: false
         ignoreCurrent: false
         ignorePowerFactor: false
       ignoreRelayStateMetric: false
