import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { TaskStatus } from 'models/Task'
import { useState } from 'react'
import { StyledDialog } from 'components/Styles/StyledComponents'

const StyledStatusDisplay = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: center;
`

const TaskStatusIcon = ({ status }: { status: TaskStatus }) => {
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

const ErrorMessageDisplay = ({ errorMessage }: { errorMessage: string }) => {
    const { TranslateText } = useLanguageContext()
    const [isOpen, setIsOpen] = useState(false)

    return (
        <>
            <Button variant="ghost" onClick={() => setIsOpen(true)}>
                {TranslateText('More info')}
            </Button>
            <StyledDialog open={isOpen} onClose={() => setIsOpen(false)}>
                <StyledDialog.Header>
                    <Typography variant="h4">{TranslateText('Error description')}</Typography>
                </StyledDialog.Header>
                <StyledDialog.Content>
                    <Typography>{errorMessage}</Typography>
                </StyledDialog.Content>
                <StyledDialog.Actions>
                    <Button variant="outlined" onClick={() => setIsOpen(false)}>
                        {TranslateText('Close')}
                    </Button>
                </StyledDialog.Actions>
            </StyledDialog>
        </>
    )
}

export const TaskStatusDisplay = ({ status, errorMessage }: { status: TaskStatus; errorMessage?: string }) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledStatusDisplay>
            <TaskStatusIcon status={status} />
            <Typography>{TranslateText(status)}</Typography>
            {errorMessage && <ErrorMessageDisplay errorMessage={errorMessage} />}
        </StyledStatusDisplay>
    )
}
