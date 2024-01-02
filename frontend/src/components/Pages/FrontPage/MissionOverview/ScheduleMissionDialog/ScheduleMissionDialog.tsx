import { EchoMissionDefinition } from 'models/MissionDefinition'
import { FetchingMissionsDialog } from './FetchingMissionsDialog'
import { NoMissionsDialog } from './NoMissionsDialog'
import { SelectMissionsToScheduleDialog } from './SelectMissionsToScheduleDialog'

export const ScheduleMissionDialog = ({
    onClose,
    echoMissions,
    isFetchingEchoMissions,
}: {
    onClose: () => void
    echoMissions: EchoMissionDefinition[]
    isFetchingEchoMissions: boolean
}): JSX.Element => {
    const isEmptyEchoMissionsDialogOpen = !isFetchingEchoMissions && echoMissions.length === 0
    const isScheduleMissionDialogOpen = !isFetchingEchoMissions && echoMissions.length !== 0

    return (
        <>
            {isFetchingEchoMissions && <FetchingMissionsDialog closeDialog={onClose} />}
            {isEmptyEchoMissionsDialogOpen && <NoMissionsDialog closeDialog={onClose} />}
            {isScheduleMissionDialogOpen && (
                <SelectMissionsToScheduleDialog echoMissionsList={echoMissions} closeDialog={onClose} />
            )}
        </>
    )
}
