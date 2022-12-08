import { Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'

interface MissionProps {
    mission?: Mission
}

const MapPositionSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 2rem;
`

const StyledPlaceholder = styled.div`
    display: flex;
    box-sizing: border-box;
    flex-direction: row;
    align-items: flex-start;
    padding: 24px;
    gap: 8px;

    border: 1px solid #dcdcdc;
    border-radius: 4px;

    flex: none;
    order: 1;
    align-self: stretch;
    flex-grow: 1;
`

function NoPositionPlaceholder() {
    return (
        <StyledPlaceholder>
            <Typography variant="h4" color="disabled">
                No robot position available
            </Typography>
        </StyledPlaceholder>
    )
}

interface TextProps {
    text: String
}

function PositionDisplay({ text }: TextProps) {
    return (
        <StyledPlaceholder>
            <Typography variant="h4" color="resting">
                Robot position: {text}
            </Typography>
        </StyledPlaceholder>
    )
}

export function MapPositionView({ mission }: MissionProps): JSX.Element {
    const [text, setText] = useState<String>('')

    useEffect(() => {
        if (mission && mission.robot.pose) {
            setText(
                `${mission.robot.pose.position.x.toFixed(2)}
                , ${mission.robot.pose.position.y.toFixed(2)}
                , ${mission.robot.pose.position.z.toFixed(2)}`
            )
        }
    }, [mission])

    return (
        <MapPositionSection>
            {text === '' && <NoPositionPlaceholder />}
            {text !== '' && <PositionDisplay text={text} />}
        </MapPositionSection>
    )
}
