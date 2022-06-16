import { Chip } from '@equinor/eds-core-react'
import { RobotStatus } from 'models/robot'
import { tokens } from '@equinor/eds-tokens'

interface StatusProps {
    status: RobotStatus
}

enum StatusColors {
    Available = '#A1DAA0',
    Offline = '#F7F7F7',
    MissionInProgress = '#FFC67A',
}

export function RobotStatusChip({ status }: StatusProps) {
    var chipColor = StatusColors.Offline
    switch (status) {
        case RobotStatus.Available: {
            chipColor = StatusColors.Available

            break
        }
        case RobotStatus.MissionInProgress: {
            chipColor = StatusColors.MissionInProgress

            break
        }
        default: {
            chipColor = StatusColors.Offline

            break
        }
    }
    return (
        <Chip
            className="StatusChip"
            style={{ background: chipColor, color: tokens.colors.text.static_icons__default.hex }}
        >
            {status}
        </Chip>
    )
}
