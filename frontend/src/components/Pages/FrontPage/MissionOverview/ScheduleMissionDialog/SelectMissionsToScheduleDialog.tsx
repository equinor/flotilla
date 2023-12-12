import { Autocomplete, Button, Card, Dialog, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useState } from 'react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { Robot } from 'models/Robot'
import { EchoMissionDefinition } from 'models/MissionDefinition'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { BackendAPICaller } from 'api/ApiCaller'

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
    echoMissions: Map<string, EchoMissionDefinition>
    closeDialog: () => void
    setLoadingMissionSet: (foo: (missionIds: Set<string>) => Set<string>) => void
}

export const SelectMissionsToScheduleDialog = ({
    echoMissions,
    closeDialog,
    setLoadingMissionSet,
}: ScheduleDialogProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useRobotContext()
    const { installationCode } = useInstallationContext()
    const [selectedEchoMissions, setSelectedEchoMissions] = useState<EchoMissionDefinition[]>([])
    const [selectedRobot, setSelectedRobot] = useState<Robot | undefined>(undefined)

    const echoMissionsOptions = Array.from(echoMissions.keys())
    const scheduleButtonDisabled = !selectedRobot || selectedEchoMissions.length === 0

    const onChangeMissionSelections = (selectedEchoMissions: string[]) => {
        var echoMissionsToSchedule: EchoMissionDefinition[] = []
        if (echoMissions) {
            selectedEchoMissions.forEach((selectedEchoMission: string) => {
                echoMissionsToSchedule.push(echoMissions.get(selectedEchoMission) as EchoMissionDefinition)
            })
        }
        setSelectedEchoMissions(echoMissionsToSchedule)
    }

    const onScheduleButtonPress = () => {
        if (!selectedRobot) return

        selectedEchoMissions.forEach((mission: EchoMissionDefinition) => {
            BackendAPICaller.postMission(mission.echoMissionId, selectedRobot.id, installationCode).catch(console.error)
            setLoadingMissionSet((currentSet: Set<string>) => {
                const updatedSet: Set<string> = new Set(currentSet)
                updatedSet.add(String(mission.name))
                return updatedSet
            })
        })

        setSelectedEchoMissions([])
        setSelectedRobot(undefined)
    }

    return (
        <StyledMissionDialog>
            <StyledDialog open={true} isDismissable>
                <StyledAutoComplete>
                    <Typography variant="h3">{TranslateText('Add mission to the queue')}</Typography>
                    <Autocomplete
                        options={echoMissionsOptions}
                        onOptionsChange={(changes) => onChangeMissionSelections(changes.selectedItems)}
                        label={TranslateText('Select missions')}
                        multiple
                        // selectedOptions={selectedEchoMissions.map((m) => m.echoMissionId + ': ' + m.name)}
                        placeholder={`${selectedEchoMissions.length}/${echoMissionsOptions.length} ${TranslateText(
                            'selected'
                        )}`}
                        autoWidth
                        onFocus={(e) => e.preventDefault()}
                    />
                    <Autocomplete
                        optionLabel={(r) => (r ? r.name + ' (' + r.model.type + ')' : '')}
                        options={enabledRobots.filter(
                            (r) =>
                                r.currentInstallation.installationCode.toLocaleLowerCase() ===
                                installationCode.toLocaleLowerCase()
                        )}
                        disabled={!enabledRobots}
                        selectedOptions={[selectedRobot]}
                        label={TranslateText('Select robot')}
                        onOptionsChange={(changes) => setSelectedRobot(changes.selectedItems[0])}
                        autoWidth
                        onFocus={(e) => e.preventDefault()}
                    />
                    <StyledMissionSection>
                        <Button onClick={closeDialog} variant="outlined">
                            {TranslateText('Cancel')}
                        </Button>
                        <Button
                            onClick={() => {
                                onScheduleButtonPress()
                                closeDialog()
                            }}
                            disabled={scheduleButtonDisabled}
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
