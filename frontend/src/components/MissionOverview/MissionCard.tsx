import { Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { ScheduledMission } from 'models/scheduledMission'
interface ScheduledMissionProps {
    scheduledMission: ScheduledMission
}

export function MissionCard({ scheduledMission }: ScheduledMissionProps) {
    return (
        <Card key={scheduledMission.id} variant="default" style={{ boxShadow: tokens.elevation.sticky }}>
            {scheduledMission.status}
        </Card>
    )
}
