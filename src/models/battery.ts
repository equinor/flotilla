export enum BatteryStatus {
    Normal = 'Normal',
    Critical = 'Critical',
    Charging = 'Charging',
    Error = 'Error',
}

export class Battery {
    status: BatteryStatus = BatteryStatus.Normal
    value?: number
    constructor(status: BatteryStatus, value?: number) {
        this.status = status
        this.value = value
    }
}
