import { Autocomplete, Button, Card, Dialog, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { memo, useState } from 'react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { Robot, RobotStatus } from 'models/Robot'
import { EchoMissionDefinition } from 'models/MissionDefinition'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'

const StyledMissionDialog = styled.div`
    display: flex;
    justify-content: space-between;
`
const StyledAutoComplete = styled(Card)`
    display: flex;
    justify-content: center;
    padding: 8px;
    gap: 25px;
    box-shadow: none;
`
const StyledMissionSection = styled.div`
    display: flex;
    margin-left: auto;
    margin-right: 0;
    gap: 10px;
`
const StyledDialog = styled(Dialog)`
    display: flex;
    padding: 1rem;
    width: 320px;
`

interface ScheduleDialogProps {
    echoMissionsList: EchoMissionDefinition[]
    closeDialog: () => void
}

export const SelectMissionsToScheduleDialog = ({ echoMissionsList, closeDialog }: ScheduleDialogProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const { setAlert } = useAlertContext()
    const { setLoadingMissionSet } = useMissionsContext()
    const [selectedEchoMissions, setSelectedEchoMissions] = useState<EchoMissionDefinition[]>([])
    const [selectedRobot, setSelectedRobot] = useState<Robot | undefined>(undefined)

    const onScheduleButtonPress = () => {
        if (!selectedRobot) return

        selectedEchoMissions.forEach((mission: EchoMissionDefinition) => {
            BackendAPICaller.postMission(mission.echoMissionId, selectedRobot.id, installationCode).catch((e) => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent
                        translatedMessage={
                            TranslateText('Failed to schedule mission') + ` '${mission.name}'. ${e.message}`
                        }
                    />,
                    AlertCategory.ERROR
                )
                setLoadingMissionSet((currentSet: Set<string>) => {
                    const updatedSet: Set<string> = new Set(currentSet)
                    updatedSet.delete(String(mission.name))
                    return updatedSet
                })
            })
            setLoadingMissionSet((currentSet: Set<string>) => {
                const updatedSet: Set<string> = new Set(currentSet)
                updatedSet.add(String(mission.name))
                return updatedSet
            })
        })

        setSelectedEchoMissions([])
        setSelectedRobot(undefined)
        closeDialog()
    }

    return (
        <StyledMissionDialog>
            <StyledDialog open={true} isDismissable>
                <StyledAutoComplete>
                    <Typography variant="h3">{TranslateText('Add mission to the queue')}</Typography>
                    <SelectMissionsComponent
                        missions={echoMissionsList}
                        selectedMissions={selectedEchoMissions}
                        setSelectedMissions={setSelectedEchoMissions}
                    />
                    <SelectRobotComponent selectedRobot={selectedRobot} setSelectedRobot={setSelectedRobot} />
                    <StyledMissionSection>
                        <Button onClick={closeDialog} variant="outlined">
                            {TranslateText('Cancel')}
                        </Button>
                        <Button
                            onClick={onScheduleButtonPress}
                            disabled={!selectedRobot || selectedEchoMissions.length === 0}
                        >
                            {' '}
                            {TranslateText('Add mission to the queue')}
                        </Button>
                    </StyledMissionSection>
                </StyledAutoComplete>
            </StyledDialog>
        </StyledMissionDialog>
    )
}

const SelectMissionsComponent = memo(
    ({
        missions,
        selectedMissions,
        setSelectedMissions,
    }: {
        missions: EchoMissionDefinition[]
        selectedMissions: EchoMissionDefinition[]
        setSelectedMissions: (missions: EchoMissionDefinition[]) => void
    }) => {
        const { TranslateText } = useLanguageContext()

        return (
            <Autocomplete
                dropdownHeight={200}
                optionLabel={(m) => m.echoMissionId + ': ' + m.name}
                options={missions}
                onOptionsChange={(changes) => setSelectedMissions(changes.selectedItems)}
                label={TranslateText('Select missions')}
                multiple
                selectedOptions={selectedMissions}
                placeholder={`${selectedMissions.length}/${missions.length} ${TranslateText('selected')}`}
                autoWidth
                onFocus={(e) => e.preventDefault()}
            />
        )
    }
)

const SelectRobotComponent = memo(
    ({
        selectedRobot,
        setSelectedRobot,
    }: {
        selectedRobot: Robot | undefined
        setSelectedRobot: (r: Robot | undefined) => void
    }) => {
        const { enabledRobots } = useRobotContext()
        const { TranslateText } = useLanguageContext()
        const { installationCode } = useInstallationContext()

        return (
            <Autocomplete
                optionLabel={(r) => (r ? r.name + ' (' + r.model.type + ')' : '')}
                options={enabledRobots.filter(
                    (r) =>
                        r.currentInstallation.installationCode.toLocaleLowerCase() ===
                            installationCode.toLocaleLowerCase() &&
                        (r.status === RobotStatus.Available || r.status === RobotStatus.Busy) &&
                        r.isarConnected
                )}
                disabled={!enabledRobots}
                selectedOptions={[selectedRobot]}
                label={TranslateText('Select robot')}
                onOptionsChange={(changes) => setSelectedRobot(changes.selectedItems[0])}
                autoWidth
                onFocus={(e) => e.preventDefault()}
            />
        )
    }
)
