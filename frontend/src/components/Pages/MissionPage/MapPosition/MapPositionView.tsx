import { Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'

interface MissionProps {
    mission?: Mission
}

const MapPositionSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 2rem;
`
const PositionCard = styled(Card)`
    padding: 16px;
`

const HorizontalContent = styled.div`
    display: grid;
    grid-template-column: auto auto auto;
    align-items: start;
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
                {Text('No robot position available')}
            </Typography>
        </StyledPlaceholder>
    )
}

interface TextProps {
    positionText: String
}

function PositionDisplay({ positionText }: TextProps) {
    return (
        <PositionCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <HorizontalContent>
                <Typography variant="h4" color="resting">
                    {positionText}
                </Typography>
            </HorizontalContent>
        </PositionCard>
    )
}

export function MapPositionView({ mission }: MissionProps): JSX.Element {
    const [positionText, setPositionText] = useState<String>('')

    useEffect(() => {
        if (mission && mission.robot.pose) {
            setPositionText(
                `${mission.robot.pose.position.x.toFixed(2)}
                , ${mission.robot.pose.position.y.toFixed(2)}
                , ${mission.robot.pose.position.z.toFixed(2)}`
            )
        }
    }, [mission])

    return (
        <MapPositionSection>
            <Typography color="resting" variant="h2">
                {Text('Robot position')}
            </Typography>
            {positionText === '' && <NoPositionPlaceholder />}
            {positionText !== '' && <PositionDisplay positionText={positionText} />}
        </MapPositionSection>
    )
}
