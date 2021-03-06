# ShellyExporters
All Shelly Exporters for Prometheus

This is a repository where I keep all my Shelly Exporters and make them available for others that look for them.  
Currently there is only one - Shelly Plug Exporter - as this is the only one I use/need.  
Other implementations were quite old and didn't give enough flexibility so I made my own in C#.

The intent is to have each exporter as a docker container since it is technically recommended by Prometheus that
one exporter should export one device.  
To me it looks quite inefficient to spin up more instances just to send a few more HTTP requests.  
But if you wish to do so then there is no problem - just configure each instance for one device.

## Shelly Plug Exporter
DockerHub: https://hub.docker.com/r/lukassoo/shelly-plug-exporter  
Docker Pull Command:
   
    docker pull lukassoo/shelly-plug-exporter
Docker Run Command:

    docker run --name ShellyPlugExporter -d -p 9918:9918 -v /etc/shellyPlugExporter:/Config lukassoo/shelly-plug-exporter
As the command implies - it will store the config in "/etc/shellyPlugExporter", change it to your liking.  
**The default config will be generated and the container will stop on the first start**

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
     
Example ready-to-go config:

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
