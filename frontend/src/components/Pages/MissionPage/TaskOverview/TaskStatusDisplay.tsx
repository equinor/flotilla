import { Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { IsarTaskStatus } from 'models/IsarTask'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'

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
            return <Icon name={Icons.Pending} style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
        }
        case IsarTaskStatus.InProgress: {
            return <Icon name={Icons.Ongoing} style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
        }
        case IsarTaskStatus.PartiallySuccessful: {
            return <Icon name={Icons.Warning} style={{ color: tokens.colors.interactive.warning__resting.rgba }} />
        }
        case IsarTaskStatus.Paused: {
            return <Icon name={Icons.Pause} style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
        }
        case IsarTaskStatus.Successful: {
            return <Icon name={Icons.Successful} style={{ color: tokens.colors.interactive.success__resting.rgba }} />
        }
    }
    return <Icon name={Icons.Failed} style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
}

export function TaskStatusDisplay({ status }: StatusProps) {
    return (
        <StyledStatusDisplay>
            {displayIcon(status)}
            <Typography>{Text(status)}</Typography>
        </StyledStatusDisplay>
    )
}
