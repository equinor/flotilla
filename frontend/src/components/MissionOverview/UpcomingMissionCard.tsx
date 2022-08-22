import { Button, Card, Checkbox, Icon, Typography } from '@equinor/eds-core-react'
import { more_vertical } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import { format, differenceInHours } from 'date-fns'
import styled from 'styled-components'
interface MissionProps {
    mission: Mission
}

const StyledMissionCard = styled(Card)`
    width: 700px;
    display: flex;
`
const HorizontalContent = styled.div`
    display: grid;
    grid-template-columns: 50px auto auto auto auto 50px;
    align-items: center;
`
const StyledMissionStartTime = styled.div`
    display: grid;
    grid-template-columns: auto auto auto;
    align-items: center;
`

Icon.add({ more_vertical })

export function UpcomingMissionCard({ mission }: MissionProps) {
    return (
        <StyledMissionCard key={mission.id} variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <HorizontalContent>
                <Checkbox />
                <Typography variant="h6">Mission name</Typography>
                <MissionStartTimeDisplay mission={mission} />
                <Typography>Tags: 17</Typography>
                <MissionDurationDisplay mission={mission} />
                <Button variant="ghost_icon">
                    <Icon name="more_vertical" size={24} title="more action" />
                </Button>
            </HorizontalContent>
        </StyledMissionCard>
    )
}

function MissionStartTimeDisplay({ mission }: MissionProps) {
    return (
        <StyledMissionStartTime>
            <Typography>Date: {format(new Date(mission.startTime), 'dd. MMM')}</Typography>
            <Typography>Time: {format(new Date(mission.startTime), 'hh:mm')}</Typography>
        </StyledMissionStartTime>
    )
}

function MissionDurationDisplay({ mission }: MissionProps) {
    return (
        <Typography>
            Estimated duration: {differenceInHours(new Date(mission.endTime), new Date(mission.startTime))} h
        </Typography>
    )
}
