import { Icon, Typography } from '@equinor/eds-core-react'
import { check_circle_outlined, error_outlined, time, warning_outlined, autorenew } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'
import { MissionStatus } from 'models/Mission'
import styled from 'styled-components'

Icon.add({ check_circle_outlined, error_outlined, warning_outlined, time, autorenew })

interface StatusProps {
    status: MissionStatus
}

const StyledStatusDisplay = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`

enum IconEnum {
    Pending = 'time',
    Ongoing = 'autorenew',
    Failed = 'error_outlined',
    Successful = 'check_circle_outlined',
}

export function displayIcon(status: MissionStatus) {
    switch (status) {
        case MissionStatus.Pending: {
            return <Icon name={IconEnum.Pending} style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
        }
        case MissionStatus.Ongoing: {
            return <Icon name={IconEnum.Ongoing} style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
        }
        case MissionStatus.Failed: {
            return <Icon name={IconEnum.Failed} style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
        }
        case MissionStatus.Successful: {
            return (
                <Icon name={IconEnum.Successful} style={{ color: tokens.colors.interactive.success__resting.rgba }} />
            )
        }
    }
    return <Icon name={IconEnum.Failed} style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
}

export function MissionStatusDisplay({ status }: StatusProps) {
    return (
        <StyledStatusDisplay>
            {displayIcon(status)}
            <Typography>{status}</Typography>
        </StyledStatusDisplay>
    )
}
