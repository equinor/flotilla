export enum RobotType {
    TaurobInspector = 'TaurobInspector',
    TaurobOperator = 'TaurobOperator',
    ExR2 = 'ExR2',
    Robot = 'Robot',
    Turtlebot = 'Turtlebot',
    AnymalX = 'AnymalX',
    AnymalD = 'AnymalD',
    NoneType = 'NoneType',
}

export interface RobotModel {
    id: string
    type: RobotType
    batteryWarningThreshold?: number
    upperPressureWarningThreshold?: number
    lowerPressureWarningThreshold?: number
}
export const placeholderRobotModel: RobotModel = {
    id: 'placeholderModelId',
    type: RobotType.Robot,
}
