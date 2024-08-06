import { Icon, Tabs, Typography, Tooltip } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { InspectionSection } from './InspectionSection'
import { useRef, useState } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { AllInspectionsTable } from './InspectionTable'
import { getInspectionDeadline } from 'utils/StringFormatting'
import styled from 'styled-components'
import { ScheduleMissionDialog } from '../FrontPage/MissionOverview/ScheduleMissionDialog/ScheduleMissionDialog'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { MissionDefinition } from 'models/MissionDefinition'
import { StyledDict } from './InspectionUtilities'
import { Icons } from 'utils/icons'
import { StyledButton } from 'components/Styles/StyledComponents'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { AlertCategory } from 'components/Alerts/AlertsBanner'

const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: end;
`

const StyledMissionButton = styled.div`
    display: flex;
    padding-bottom: 30px;
`

const StyledPlaceholderContent = styled.div`
    width: 100%;
`

const StyledView = styled.div`
    display: flex;
    align-items: flex-start;
`

const AlignedTextButton = styled(StyledButton)`
    text-align: left;
`

export const InspectionOverviewSection = () => {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const { enabledRobots } = useRobotContext()
    const { setAlert, setListAlert } = useAlertContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const [isFetchingMissions, setIsFetchingMissions] = useState<boolean>(false)
    const [isScheduleMissionDialogOpen, setIsScheduleMissionDialogOpen] = useState<boolean>(false)
    const [missions, setMissions] = useState<MissionDefinition[]>([])
    const [activeTab, setActiveTab] = useState(0)

    const isScheduleButtonDisabled = enabledRobots.length === 0 || installationCode === ''

    const anchorRef = useRef<HTMLButtonElement>(null)

    const allInspections = missionDefinitions.map((m) => {
        return {
            missionDefinition: m,
            deadline: m.lastSuccessfulRun
                ? getInspectionDeadline(m.inspectionFrequency, m.lastSuccessfulRun.endTime!)
                : undefined,
        }
    })

    const fetchMissions = () => {
        setIsFetchingMissions(true)
        BackendAPICaller.getAvailableMissions(installationCode as string)
            .then((missions) => {
                setMissions(missions)
                setIsFetchingMissions(false)
            })
            .catch((_) => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent translatedMessage={TranslateText('Failed to retrieve missions')} />,
                    AlertCategory.ERROR
                )
                setListAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertListContent translatedMessage={TranslateText('Failed to retrieve missions')} />,
                    AlertCategory.ERROR
                )
                setIsFetchingMissions(false)
            })
    }

    const onClickScheduleMission = () => {
        setIsScheduleMissionDialogOpen(true)
        fetchMissions()
    }

    const AddPredefinedMissionsButton = () => (
        <Tooltip placement="top" title={isScheduleButtonDisabled ? TranslateText('No robot available') : ''}>
            <AlignedTextButton onClick={onClickScheduleMission} disabled={isScheduleButtonDisabled} ref={anchorRef}>
                <Icon name={Icons.Add} size={16} />
                {TranslateText('Add predefined mission')}
            </AlignedTextButton>
        </Tooltip>
    )

    return (
        <Tabs activeTab={activeTab} onChange={setActiveTab}>
            <Tabs.List>
                <Tabs.Tab>{TranslateText('Deck Overview')}</Tabs.Tab>
                <Tabs.Tab>{TranslateText('Predefined Missions')}</Tabs.Tab>
            </Tabs.List>
            <Tabs.Panels>
                <Tabs.Panel>
                    <InspectionSection />
                </Tabs.Panel>
                <Tabs.Panel>
                    <StyledView>
                        <StyledContent>
                            <StyledMissionButton>
                                {isScheduleMissionDialogOpen && (
                                    <ScheduleMissionDialog
                                        isFetchingMissions={isFetchingMissions}
                                        missions={missions}
                                        onClose={() => setIsScheduleMissionDialogOpen(false)}
                                    />
                                )}
                                <AddPredefinedMissionsButton />
                            </StyledMissionButton>
                            {allInspections.length > 0 ? (
                                <AllInspectionsTable inspections={allInspections} />
                            ) : (
                                <StyledPlaceholderContent>
                                    <StyledDict.Placeholder>
                                        <Typography variant="h4" color="disabled">
                                            {TranslateText('No predefined missions available')}
                                        </Typography>
                                    </StyledDict.Placeholder>
                                </StyledPlaceholderContent>
                            )}
                        </StyledContent>
                    </StyledView>
                </Tabs.Panel>
            </Tabs.Panels>
        </Tabs>
    )
}
