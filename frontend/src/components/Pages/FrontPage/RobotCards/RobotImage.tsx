import { RobotType } from 'models/RobotModel'
import taurobInspector from 'mediaAssets/taurob_inspector.jpg'
import anymalX from 'mediaAssets/anymal_x.png'
import anymalD from 'mediaAssets/anymal_d.png'
import exRobotics from 'mediaAssets/ExRobotics.webp'
import turtleBot from 'mediaAssets/turtlebot.webp'
import robot from 'mediaAssets/robot.png'
import styled from 'styled-components'
import { Icon } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
interface TypeProps {
    robotType?: RobotType
    height?: string
}

const StyledImage = styled.img<{ $height?: string }>`
    height: ${(props) => props.$height};
`

const StyledIcon = styled(Icon)`
    display: flex;
    justify-content: center;
    height: 200px;
    width: 100%;
    scale: 50%;
    color: #6f6f6f;
`

export function RobotImage({ robotType, height = '200px' }: TypeProps) {
    var robotImage
    switch (robotType) {
        case RobotType.TaurobInspector: {
            robotImage = taurobInspector
            break
        }
        case RobotType.ExR2: {
            robotImage = exRobotics
            break
        }
        case RobotType.Turtlebot: {
            robotImage = turtleBot
            break
        }
        case RobotType.Robot: {
            robotImage = robot
            break
        }
        case RobotType.AnymalX: {
            robotImage = anymalX
            break
        }
        case RobotType.AnymalD: {
            robotImage = anymalD
            break
        }
        case RobotType.NoneType: {
            return <StyledIcon name={Icons.CloudOff} title={robotType} />
        }
        default: {
            return <StyledIcon name={Icons.Image} title={robotType} />
        }
    }
    return <StyledImage height={height} alt={robotType} src={robotImage} />
}
