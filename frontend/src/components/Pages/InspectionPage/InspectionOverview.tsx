import { Button, Tabs, Icon } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { RefreshProps } from '../FrontPage/FrontPage'
import { DeckMissionType, InspectionSection, OngoingMissionType } from './InspectionSection'
import { useEffect, useRef, useState } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { AllInspectionsTable } from './InspectionTable'
import { getInspectionDeadline } from 'utils/StringFormatting'
import { Inspection } from './InspectionSection'
import styled from 'styled-components'
import { useContext } from 'react'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { Icons } from 'utils/icons'
import { MissionStatus } from 'models/Mission'

const StyledButton = styled(Button)`
    display: flex;
    align-items: center;
    gap: 8px;
    border-radius: 4px;
`

const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: end;
    gap: 20px;
`

const StyledView = styled.div`
    display: flex;
    align-items: flex-start;
`

export function InspectionOverviewSection({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const [activeTab, setActiveTab] = useState(0)
    const [allMissions, setAllMissions] = useState<Inspection[]>()
    const installationCode = useContext(InstallationContext).installationCode
    const echoURL = 'https://echo.equinor.com/missionplanner?instCode='
    const anchorRef = useRef<HTMLButtonElement>(null)
    const [ongoingMissions, setOngoingMissions] = useState<OngoingMissionType>({})
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)

    const handleChange = (index: number) => {
        setActiveTab(index)
    }

    const updateOngoingMissionsMap = async (areaMissions: DeckMissionType) => {
        let newOngoingMissions: OngoingMissionType = {}
        for (const areaId of Object.keys(areaMissions)) {
            for (const inspection of areaMissions[areaId].inspections) {
                const missionDefinition = inspection.missionDefinition
                const missionRuns = await BackendAPICaller.getMissionRuns({
                    statuses: [MissionStatus.Paused, MissionStatus.Pending, MissionStatus.Ongoing],
                    missionId: missionDefinition.id,
                })
                newOngoingMissions[missionDefinition.id] = missionRuns.content.length > 0
            }
        }
        setOngoingMissions(newOngoingMissions)
    }

    useEffect(() => {
        const fetchMissionDefinitions = async () => {
            let missionDefinitions = await BackendAPICaller.getMissionDefinitions({ pageSize: 100 }).then(
                (response) => response.content
            )
            if (!missionDefinitions) missionDefinitions = []
            let newInspection: Inspection[] = missionDefinitions.map((m) => {
                return {
                    missionDefinition: m,
                    deadline: m.lastRun ? getInspectionDeadline(m.inspectionFrequency, m.lastRun.endTime!) : undefined,
                }
            })

            setAllMissions(newInspection)
        }
        fetchMissionDefinitions()
    }, [activeTab])

    return (
        <Tabs activeTab={activeTab} onChange={handleChange}>
            <Tabs.List>
                <Tabs.Tab>{TranslateText('Deck Overview')}</Tabs.Tab>
                <Tabs.Tab>{TranslateText('Predefined missions')}</Tabs.Tab>
            </Tabs.List>
            <Tabs.Panels>
                <Tabs.Panel>
                    <InspectionSection
                        refreshInterval={refreshInterval}
                        updateOngoingMissionsMap={updateOngoingMissionsMap}
                        ongoingMissions={ongoingMissions}
                    />
                </Tabs.Panel>
                <Tabs.Panel>
                    <StyledView>
                        <StyledContent>
                            <StyledButton
                                variant="outlined"
                                onClick={() => {
                                    window.open(echoURL + installationCode)
                                }}
                                disabled={installationCode === ''}
                                ref={anchorRef}
                            >
                                <Icon name={Icons.ExternalLink} size={16}></Icon>
                                {TranslateText('Create a new mission in the Mission Planner')}
                            </StyledButton>
                            {allMissions != null && (
                                <AllInspectionsTable
                                    inspections={allMissions}
                                    ongoingMissions={ongoingMissions}
                                    isDialogOpen={isDialogOpen}
                                    openDialog={() => setIsDialogOpen(true)}
                                    closeDialog={() => setIsDialogOpen(false)}
                                />
                            )}
                        </StyledContent>
                    </StyledView>
                </Tabs.Panel>
            </Tabs.Panels>
        </Tabs>
    )
}
