import { Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { MissionStatus } from 'models/Mission'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'

interface StatusProps {
    status: MissionStatus
}

const StyledStatusDisplay = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: space-between;
`

const StyledStatusIcon = styled.div`
    display: flex;
    gap: 4px;
    align-items: flex-end;
`

export function displayIcon(status: MissionStatus) {
    switch (status) {
        case MissionStatus.Pending: {
            return <Icon name={Icons.Pending} style={{ color: tokens.colors.text.static_icons__secondary.hex }} />
        }
        case MissionStatus.Ongoing: {
            return <Icon name={Icons.Ongoing} style={{ color: tokens.colors.text.static_icons__secondary.hex }} />
        }
        case MissionStatus.Failed: {
            return <Icon name={Icons.Failed} style={{ color: tokens.colors.interactive.danger__resting.hex }} />
        }
        case MissionStatus.Successful: {
            return <Icon name={Icons.Successful} style={{ color: tokens.colors.interactive.success__resting.hex }} />
        }
        case MissionStatus.PartiallySuccessful: {
            return <Icon name={Icons.Warning} style={{ color: tokens.colors.interactive.warning__resting.hex }} />
        }
    }
    return <Icon name={Icons.Failed} style={{ color: tokens.colors.interactive.danger__resting.hex }} />
}

export function MissionStatusDisplay({ status }: StatusProps) {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledStatusIcon>
            {displayIcon(status)}
            <Typography>{TranslateText(status)}</Typography>
        </StyledStatusIcon>
    )
}

export function MissionStatusDisplayWithHeader({ status }: StatusProps) {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledStatusDisplay>
            <Typography variant="meta" color={tokens.colors.text.static_icons__tertiary.hex}>
                {TranslateText('Status')}
            </Typography>
            <MissionStatusDisplay status={status} />
        </StyledStatusDisplay>
    )
}
