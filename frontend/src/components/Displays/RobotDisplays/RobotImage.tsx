import { RobotType } from 'models/Robot'
import taurobInspector from 'mediaAssets/taurob_inspector_no_background.png'
import anymalX from 'mediaAssets/anymal_x.png'
import anymalD from 'mediaAssets/anymal_d.png'
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
    width: auto;
    max-width: 100%;
    object-fit: contain;
`
const StyledIcon = styled(Icon)`
    display: flex;
    justify-content: center;
    width: 100%;
    scale: 50%;
    color: #6f6f6f;
`
const ContainIcon = styled.div`
    display: block;
`

export const RobotImage = ({ robotType, height = '200px' }: TypeProps) => {
    let robotImage
    switch (robotType) {
        case RobotType.TaurobInspector: {
            robotImage = taurobInspector
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
            return (
                <ContainIcon>
                    <StyledIcon name={Icons.CloudOff} title={robotType} style={{ minHeight: height }} />
                </ContainIcon>
            )
        }
        default: {
            return (
                <ContainIcon>
                    <StyledIcon name={Icons.Image} title={robotType} style={{ minHeight: height }} />
                </ContainIcon>
            )
        }
    }
    return <StyledImage $height={height} alt={robotType} src={robotImage} />
}
