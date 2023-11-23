import { Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { TaskStatus } from 'models/Task'

interface StatusProps {
    status: TaskStatus
}

const StyledStatusDisplay = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`

const TaskStatusIcon = ({ status }: StatusProps) => {
    switch (status) {
        case TaskStatus.NotStarted: {
            return <Icon name={Icons.Pending} style={{ color: tokens.colors.text.static_icons__secondary.hex }} />
        }
        case TaskStatus.InProgress: {
            return <Icon name={Icons.Ongoing} style={{ color: tokens.colors.text.static_icons__secondary.hex }} />
        }
        case TaskStatus.PartiallySuccessful: {
            return <Icon name={Icons.Warning} style={{ color: tokens.colors.interactive.warning__resting.hex }} />
        }
        case TaskStatus.Paused: {
            return <Icon name={Icons.Pause} style={{ color: tokens.colors.text.static_icons__secondary.hex }} />
        }
        case TaskStatus.Successful: {
            return <Icon name={Icons.Successful} style={{ color: tokens.colors.interactive.success__resting.hex }} />
        }
    }
    return <Icon name={Icons.Failed} style={{ color: tokens.colors.interactive.danger__resting.hex }} />
}

export function TaskStatusDisplay({ status }: StatusProps) {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledStatusDisplay>
            <TaskStatusIcon status={status} />
            <Typography>{TranslateText(status)}</Typography>
        </StyledStatusDisplay>
    )
}
