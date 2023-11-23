import { Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { Mission, MissionStatus } from 'models/Mission'
import styled from 'styled-components'

const StyledCard = styled(Card)`
    width: fit-content;
    padding: 7px 15px;
    gap: 0.2rem;
`

interface MissionProps {
    mission: Mission
}

export const StatusReason = ({ mission }: MissionProps) => {
    if (!mission.statusReason) return <></>

    var warningLevel: 'default' | 'info' | 'warning' | 'danger' = 'info'
    switch (mission.status) {
        case MissionStatus.Failed:
            warningLevel = 'danger'
            break
        case MissionStatus.Aborted:
            warningLevel = 'danger'
            break
        case MissionStatus.Cancelled:
            warningLevel = 'warning'
            break
        case MissionStatus.PartiallySuccessful:
            warningLevel = 'warning'
            break
    }

    return (
        <StyledCard variant={warningLevel} style={{ boxShadow: tokens.elevation.raised }}>
            <Typography variant="h5">{mission.statusReason}</Typography>
        </StyledCard>
    )
}
