import { FetchingMissionsDialog } from './FetchingMissionsDialog'
import { NoMissionsDialog } from './NoMissionsDialog'
import { SelectMissionsToScheduleDialog } from './SelectMissionsToScheduleDialog'
import { CondensedMissionDefinition } from 'models/CondensedMissionDefinition'

export const ScheduleMissionDialog = ({
    onClose,
    missions,
    isFetchingMissions,
}: {
    onClose: () => void
    missions: CondensedMissionDefinition[]
    isFetchingMissions: boolean
}) => {
    const isEmptyMissionsDialogOpen = !isFetchingMissions && missions.length === 0
    const isScheduleMissionDialogOpen = !isFetchingMissions && missions.length !== 0

    return (
        <>
            {isFetchingMissions && <FetchingMissionsDialog closeDialog={onClose} />}
            {isEmptyMissionsDialogOpen && <NoMissionsDialog closeDialog={onClose} />}
            {isScheduleMissionDialogOpen && (
                <SelectMissionsToScheduleDialog missionsList={missions} closeDialog={onClose} />
            )}
        </>
    )
}
