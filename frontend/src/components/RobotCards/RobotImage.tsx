import { RobotType } from 'models/robot'
import taurobInspector from 'mediaAssets/taurob_inspector.jpg'
import exRobotics from 'mediaAssets/ExRobotics.webp'
import turtleBot from 'mediaAssets/turtlebot.webp'
import styled from 'styled-components'
import { Icon } from '@equinor/eds-core-react'
import { cloud_off, image } from '@equinor/eds-icons'

Icon.add({ cloud_off, image })
interface TypeProps {
    robotType?: RobotType
}

const StyledImage = styled.img`
    object-fit: contain;
    height: 200px;
    width: 100%;
`

const StyledIcon = styled(Icon)`
    display: flex;
    justify-content: center;
    height: 200px;
    width: 100%;
    scale: 50%;
    color: #6f6f6f;
`

export function RobotImage({ robotType }: TypeProps) {
    var robotImage
    switch (robotType) {
        case RobotType.Taurob: {
            robotImage = taurobInspector
            break
        }
        case RobotType.ExRobotics: {
            robotImage = exRobotics
            break
        }
        case RobotType.TurtleBot: {
            robotImage = turtleBot
            break
        }
        case RobotType.NoneType: {
            return <StyledIcon name="cloud_off" title={robotType} />
        }
        default: {
            return <StyledIcon name="image" title={robotType} />
        }
    }
    return <StyledImage alt={robotType} src={robotImage} />
}
