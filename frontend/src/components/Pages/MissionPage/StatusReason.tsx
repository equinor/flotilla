import { Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { MissionStatus } from 'models/Mission'
import styled from 'styled-components'

const StyledCard = styled(Card)`
    width: fit-content;
    padding: 7px 15px;
    gap: 0.2rem;
`

interface MissionProps {
    statusReason: string | undefined
    status: MissionStatus
}

export const StatusReason = ({ statusReason, status }: MissionProps) => {
    if (!statusReason) return <></>

    let warningLevel: 'default' | 'info' | 'warning' | 'danger' = 'info'
    switch (status) {
        case MissionStatus.Failed:
        case MissionStatus.Aborted:
            warningLevel = 'danger'
            break
        case MissionStatus.Cancelled:
        case MissionStatus.PartiallySuccessful:
            warningLevel = 'warning'
            break
    }

    return (
        <StyledCard variant={warningLevel} style={{ boxShadow: tokens.elevation.raised }}>
            <Typography variant="h5">{statusReason}</Typography>
        </StyledCard>
    )
}
