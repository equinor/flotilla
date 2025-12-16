import { Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { MissionStatus } from 'models/Mission'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { AttributeTitleTypography } from 'components/Styles/StyledComponents'

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
    gap: 6px;
    align-items: center;
`
const StyledIcon = styled(Icon)`
    width: 20px;
    height: 20px;
`

const MissionStatusIcon = ({ status }: StatusProps) => {
    switch (status) {
        case MissionStatus.Queued: {
            return (
                <StyledIcon
                    name={Icons.Queued}
                    style={{ color: tokens.colors.text.static_icons__secondary.hex }}
                    size={18}
                />
            )
        }
        case MissionStatus.Pending: // Here we could have a different "mission starting" icon
        case MissionStatus.Ongoing: {
            return <StyledIcon name={Icons.Ongoing} style={{ color: tokens.colors.text.static_icons__secondary.hex }} />
        }
        case MissionStatus.Successful: {
            return (
                <StyledIcon name={Icons.Successful} style={{ color: tokens.colors.interactive.success__resting.hex }} />
            )
        }
        case MissionStatus.PartiallySuccessful:
        case MissionStatus.Cancelled: {
            return <StyledIcon name={Icons.Warning} style={{ color: tokens.colors.interactive.warning__resting.hex }} />
        }
        case MissionStatus.Failed: {
            return <StyledIcon name={Icons.Failed} style={{ color: tokens.colors.interactive.danger__resting.hex }} />
        }
    }
    return <StyledIcon name={Icons.Failed} style={{ color: tokens.colors.interactive.danger__resting.hex }} />
}

export const MissionStatusDisplay = ({ status }: StatusProps) => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledStatusIcon>
            <MissionStatusIcon status={status} />
            <Typography>{TranslateText(status)}</Typography>
        </StyledStatusIcon>
    )
}

export const MissionStatusDisplayShort = ({ status }: StatusProps) => {
    return (
        <StyledStatusIcon>
            <MissionStatusIcon status={status} />
        </StyledStatusIcon>
    )
}

export const MissionStatusDisplayWithHeader = ({ status }: StatusProps) => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledStatusDisplay>
            <AttributeTitleTypography>{TranslateText('Status')}</AttributeTitleTypography>
            <MissionStatusDisplay status={status} />
        </StyledStatusDisplay>
    )
}
