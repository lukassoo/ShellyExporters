﻿namespace ShellyPlusPmMiniExporter;

public class TargetDevice
{
    public string name;
    public string url;
    public string password;
    
    public float requestTimeoutTime = 3;
    
    public bool ignoreTotalPowerMetric;
    public bool ignorePowerMetric;
    public bool ignoreVoltageMetric;
    public bool ignoreCurrentMetric;

    public bool ignoreInputState;
    public bool ignoreInputPercent;
    public bool ignoreInputCountTotal;
    public bool ignoreInputFrequency;
    
    // Parameterless constructor for deserialization
    public TargetDevice()
    {
        name = "";
        url = "";
        password = "";
    }

    public TargetDevice(string name, string url, string password)
    {
        this.name = name;
        this.url = url;
        this.password = password;
    }

    public bool RequiresAuthentication()
    {
        return !string.IsNullOrEmpty(password);
    }

    public bool NeedsInputStatusRequests()
    {
        return ignoreInputState || ignoreInputPercent || ignoreInputCountTotal || ignoreInputFrequency;
    }
}
