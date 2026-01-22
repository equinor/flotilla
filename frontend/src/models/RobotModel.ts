export enum RobotType {
    TaurobInspector = 'TaurobInspector',
    TaurobOperator = 'TaurobOperator',
    Robot = 'Robot',
    Turtlebot = 'Turtlebot',
    AnymalX = 'AnymalX',
    AnymalD = 'AnymalD',
    NoneType = 'NoneType',
}

export interface RobotModel {
    id: string
    type: RobotType
}
export const placeholderRobotModel: RobotModel = {
    id: 'placeholderModelId',
    type: RobotType.Robot,
}
