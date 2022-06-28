import { Icon, Typography } from '@equinor/eds-core-react'
import { check_circle_outlined, error_outlined, time, warning_outlined } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'
import { MissionStatus } from 'models/mission'
import styled from 'styled-components'

Icon.add({ check_circle_outlined, error_outlined, warning_outlined, time })

interface StatusProps {
    status: MissionStatus
}

const StyledStatusDisplay = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`

function displayIcon(status: MissionStatus) {
    switch (status) {
        case MissionStatus.Pending: {
            return <Icon name="time" style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
        }
        case MissionStatus.Started: {
            return <Icon name="time" style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
        }
        case MissionStatus.Warning: {
            return <Icon name="warning_outlined" style={{ color: tokens.colors.interactive.warning__resting.rgba }} />
        }
        case MissionStatus.Successful: {
            return (
                <Icon name="check_circle_outlined" style={{ color: tokens.colors.interactive.success__resting.rgba }} />
            )
        }
    }
    return <Icon name="error_outlined" style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
}

export function MissionStatusDisplay({ status }: StatusProps) {
    return (
        <StyledStatusDisplay>
            {displayIcon(status)}
            <Typography>{status}</Typography>
        </StyledStatusDisplay>
    )
}
