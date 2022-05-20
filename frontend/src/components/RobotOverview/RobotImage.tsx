import { RobotType } from 'models/robot'
import taurobInspector from 'mediaAssets/taurob_inspector.jpg'
import exRobotics from 'mediaAssets/ExRobotics.webp'
import turtleBot from 'mediaAssets/turtlebot.webp'

interface TypeProps {
    robotType: RobotType
}

export function RobotImage({ robotType }: TypeProps) {
    var image
    switch (robotType) {
        case RobotType.Taurob: {
            image = taurobInspector
            break
        }
        case RobotType.ExRobotics: {
            image = exRobotics
            break
        }
        case RobotType.TurtleBot: {
            image = turtleBot
            break
        }
        default: {
            break
        }
    }
    console.log(image)
    return <img alt={robotType} src={image} />
}
