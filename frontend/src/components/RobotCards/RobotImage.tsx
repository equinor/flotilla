import { RobotType } from 'models/robot'
import taurobInspector from 'mediaAssets/taurob_inspector.jpg'
import exRobotics from 'mediaAssets/ExRobotics.webp'
import turtleBot from 'mediaAssets/turtlebot.webp'
import styled from 'styled-components'

interface TypeProps {
    robotType: RobotType
}

const StyledImage = styled.img`
    object-fit: contain;
    height: 200px;
    width: 100%;
`

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
    return <StyledImage alt={robotType} src={image} />
}
