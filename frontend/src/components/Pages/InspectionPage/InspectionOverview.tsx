import { Button, Tabs, Icon } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { InspectionSection, ScheduledMissionType } from './InspectionSection'
import { useEffect, useRef, useState } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { AllInspectionsTable } from './InspectionTable'
import { getInspectionDeadline } from 'utils/StringFormatting'
import { Inspection } from './InspectionSection'
import styled from 'styled-components'
import { useContext } from 'react'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { Icons } from 'utils/icons'
import { useMissionsContext } from 'components/Contexts/MissionListsContext'

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

export function InspectionOverviewSection() {
    const { TranslateText } = useLanguageContext()
    const [activeTab, setActiveTab] = useState(0)
    const [allMissions, setAllMissions] = useState<Inspection[]>()
    const [ongoingMissionsMap, setOngoingMissions] = useState<ScheduledMissionType>({})
    const [scheduledMissions, setScheduledMissions] = useState<ScheduledMissionType>({})
    const { ongoingMissions, missionQueue } = useMissionsContext()
    const installationCode = useContext(InstallationContext).installationCode
    const echoURL = 'https://echo.equinor.com/missionplanner?instCode='
    const anchorRef = useRef<HTMLButtonElement>(null)

    useEffect(() => {
        setOngoingMissions(() => {
            const newOngoingMissionsMap: ScheduledMissionType = {}
            ongoingMissions.forEach((m) =>
                newOngoingMissionsMap[m.missionId!] = true
            )
            return newOngoingMissionsMap
        })
    }, [ongoingMissions])

    useEffect(() => {
        setScheduledMissions(() => {
            const newScheduledMissions: ScheduledMissionType = {}
            missionQueue.forEach((m) =>
                newScheduledMissions[m.missionId!] = true
            )
            return newScheduledMissions
        })
        
    }, [missionQueue])

    useEffect(() => {
        const fetchMissionDefinitions = async () => {
            let missionDefinitions = await BackendAPICaller.getMissionDefinitions({ pageSize: 100 }).then(
                (response) => response.content
            )
            if (!missionDefinitions) missionDefinitions = []
            let newInspection: Inspection[] = missionDefinitions.map((m) => {
                return {
                    missionDefinition: m,
                    deadline: m.lastSuccessfulRun ? getInspectionDeadline(m.inspectionFrequency, m.lastSuccessfulRun.endTime!) : undefined,
                }
            })

            setAllMissions(newInspection)
        }
        fetchMissionDefinitions()
    }, [activeTab])

    return (
        <Tabs activeTab={activeTab} onChange={setActiveTab}>
            <Tabs.List>
                <Tabs.Tab>{TranslateText('Deck Overview')}</Tabs.Tab>
                <Tabs.Tab>{TranslateText('Predefined Missions')}</Tabs.Tab>
            </Tabs.List>
            <Tabs.Panels>
                <Tabs.Panel>
                    <InspectionSection
                        scheduledMissions={scheduledMissions}
                        ongoingMissions={ongoingMissionsMap}
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
                            {allMissions && (
                                <AllInspectionsTable
                                    inspections={allMissions}
                                    scheduledMissions={scheduledMissions}
                                    ongoingMissions={ongoingMissionsMap}
                                />
                            )}
                        </StyledContent>
                    </StyledView>
                </Tabs.Panel>
            </Tabs.Panels>
        </Tabs>
    )
}
