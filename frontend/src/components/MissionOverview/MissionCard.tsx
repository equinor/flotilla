import { Button, Card, Checkbox, Icon, Typography } from '@equinor/eds-core-react'
import { more_vertical } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/mission'
import styled from 'styled-components'
interface MissionProps {
    mission: Mission
}

const StyledMissionCard = styled(Card)`
    width: 600px;
    display: flex;
`
const StyledMissionCardLeft = styled(Card)`
    width: 200px;
    display: flex;
    justify-content: flex-start;
`
const StyledMissionCardRight = styled(Card)`
    width: 400px;
    display: flex;
    justify-content: flex-end;
`

Icon.add({ more_vertical })

export function MissionCard({ mission }: MissionProps) {
    return (
        <StyledMissionCard key={mission.id} variant="default" style={{ boxShadow: tokens.elevation.sticky }}>
            <StyledMissionCardLeft>
                <Checkbox />
                <Typography variant="body_short_bold">{mission.isarMissionId}</Typography>
            </StyledMissionCardLeft>
            <StyledMissionCardRight>
                <Typography>Start: 00:00</Typography>
                <Typography>Tags: 15</Typography>
                <Typography>Estimated duration: 1h</Typography>
                <Button variant="ghost_icon">
                    <Icon name="more_vertical" size={24} title="more action" />
                </Button>
            </StyledMissionCardRight>
        </StyledMissionCard>
    )
}
