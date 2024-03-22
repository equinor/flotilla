import { Chip } from '@equinor/eds-core-react'
import { RobotStatus } from 'models/Robot'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useSafeZoneContext } from 'components/Contexts/SafeZoneContext'

interface StatusProps {
    status?: RobotStatus
    isarConnected: boolean
}

enum StatusColors {
    Available = '#A1DAA0',
    Offline = '#F7F7F7',
    Busy = '#FFC67A',
    Blocked = '#FFC67A',
    SafeZone = '#FF0000',
    ConnetionIssues = '#F7F7F7',
}

export const RobotStatusChip = ({ status, isarConnected }: StatusProps) => {
    const { TranslateText } = useLanguageContext()
    const { safeZoneStatus } = useSafeZoneContext()

    var chipColor = StatusColors.Offline
    switch (status) {
        case RobotStatus.Available: {
            chipColor = StatusColors.Available
            break
        }
        case RobotStatus.Busy: {
            chipColor = StatusColors.Busy
            break
        }
        case RobotStatus.Blocked: {
            chipColor = StatusColors.Blocked
            break
        }
        default: {
            chipColor = StatusColors.Offline
            status = RobotStatus.Offline
            break
        }
    }

    if (!isarConnected) {
        chipColor = StatusColors.ConnetionIssues
        status = RobotStatus.ConnectionIssues
    } else if (safeZoneStatus) {
        chipColor = StatusColors.SafeZone
        status = RobotStatus.SafeZone
    }

    return (
        <Chip
            className="StatusChip"
            style={{ background: chipColor, color: tokens.colors.text.static_icons__default.hex }}
        >
            {TranslateText(status)}
        </Chip>
    )
}
