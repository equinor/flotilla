import { Chip } from '@equinor/eds-core-react'
import { RobotStatus } from 'models/Robot'
import { tokens } from '@equinor/eds-tokens'
import { TranslateText } from 'components/Contexts/LanguageContext'

interface StatusProps {
    status?: RobotStatus
}

enum StatusColors {
    Available = '#A1DAA0',
    Offline = '#F7F7F7',
    Busy = '#FFC67A',
}

export function RobotStatusChip({ status }: StatusProps) {
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
        default: {
            chipColor = StatusColors.Offline
            status = RobotStatus.Offline
            break
        }
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
