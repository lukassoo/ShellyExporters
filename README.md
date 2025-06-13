# ShellyExporters
All Shelly Exporters for Prometheus that I have written or others have contributed.

This repository started with me looking to export my Shelly devices using Prometheus. Other implementations were quite old and/or didn't give enough flexibility, so I wrote my own in C#.

Each exporter is available as a docker container

## [Documentation](https://github.com/lukassoo/ShellyExporters/wiki)

Visit the wiki for documentation

## Exporter Requests

If you would like me to write exporters for more Shelly devices, I am happy to do so when I have some spare time.  
Open a new issue, and we can discuss it.  
You may also create a pull request if you can do it yourself and understand the architecture that I am trying to maintain.

For me to write another exporter, I will need:
- Access to the device for testing (simple port forward), or, you willing to test builds/containers on your end.
- A few days of time - I may not have much free time at a given moment, testing exporters can take some time when there are issues.

## Example Grafana Dashboard Showcase

Shelly Plug
![Shelly Plug](https://github.com/lukassoo/ShellyExporters/wiki/images/shellyPlugDashboard.png)

Shelly Plus Plug
![Shelly Plus Plug](https://github.com/lukassoo/ShellyExporters/wiki/images/shellyPlusPlugDashboard.png)

Shelly 3 EM
![Shelly 3 EM](https://raw.githubusercontent.com/wiki/lukassoo/ShellyExporters/images/shelly3EmDashboard.png)

Shelly Pro 3 EM
![Shelly Pro 3 EM](https://github.com/lukassoo/ShellyExporters/wiki/images/shellyPro3EmDashboard.png)
