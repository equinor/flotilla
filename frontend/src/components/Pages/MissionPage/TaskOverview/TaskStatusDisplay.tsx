import { Icon, Typography } from '@equinor/eds-core-react'
import {
    check_circle_outlined,
    error_outlined,
    time,
    warning_outlined,
    autorenew,
    pause_circle_outlined,
} from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'
import { IsarTaskStatus } from 'models/IsarTask'
import styled from 'styled-components'

Icon.add({ check_circle_outlined, error_outlined, warning_outlined, time, autorenew, pause_circle_outlined })

interface StatusProps {
    status: IsarTaskStatus
}

const StyledStatusDisplay = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`

function displayIcon(status: IsarTaskStatus) {
    switch (status) {
        case IsarTaskStatus.NotStarted: {
            return <Icon name="time" style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
        }
        case IsarTaskStatus.InProgress: {
            return <Icon name="autorenew" style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
        }
        case IsarTaskStatus.PartiallySuccessful: {
            return <Icon name="warning_outlined" style={{ color: tokens.colors.interactive.warning__resting.rgba }} />
        }
        case IsarTaskStatus.Paused: {
            return (
                <Icon name="pause_circle_outlined" style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
            )
        }
        case IsarTaskStatus.Successful: {
            return (
                <Icon name="check_circle_outlined" style={{ color: tokens.colors.interactive.success__resting.rgba }} />
            )
        }
    }
    return <Icon name="error_outlined" style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
}

export function TaskStatusDisplay({ status }: StatusProps) {
    return (
        <StyledStatusDisplay>
            {displayIcon(status)}
            <Typography>{status}</Typography>
        </StyledStatusDisplay>
    )
}
