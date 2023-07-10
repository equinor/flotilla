export enum RobotType {
    TaurobInspector = 'TaurobInspector',
    TaurobOperator = 'TaurobOperator',
    ExR2 = 'ExR2',
    Turtlebot = 'Turtlebot',
    Robot = 'Robot',
    AnymalX = 'AnymalX',
    AnymalD = 'AnymalD',
    NoneType = 'NoneType',
}

export interface RobotModel {
    id: string
    type: RobotType
    batteryWarningThreshold?: number
    upperPressureWarningThreshold: number
    lowerPressureWarningThreshold: number
}
