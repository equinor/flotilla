import { Tabs } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { InspectionSection } from './InspectionSection'
import { useEffect, useState } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { AllInspectionsTable } from './InspectionTable'
import { getInspectionDeadline } from 'utils/StringFormatting'
import { Inspection } from './InspectionSection'
import styled from 'styled-components'
import { ScheduleMissionDialog } from '../FrontPage/MissionOverview/ScheduleMissionDialog/ScheduleMissionDialog'
import { MissionButton } from 'components/Displays/MissionButtons/MissionButton'

const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: end;
`

const StyledButtons = styled.div`
    display: flex;
    flex-direction: row;
    gap: 8px;
    padding-bottom: 30px;
`

const StyledView = styled.div`
    display: flex;
    align-items: flex-start;
`

export const InspectionOverviewSection = () => {
    const { TranslateText } = useLanguageContext()
    const [activeTab, setActiveTab] = useState(0)
    const [allMissions, setAllMissions] = useState<Inspection[]>()

    useEffect(() => {
        const fetchMissionDefinitions = async () => {
            let missionDefinitions = await BackendAPICaller.getMissionDefinitions({ pageSize: 100 }).then(
                (response) => response.content
            )
            if (!missionDefinitions) missionDefinitions = []
            let newInspection: Inspection[] = missionDefinitions.map((m) => {
                return {
                    missionDefinition: m,
                    deadline: m.lastSuccessfulRun
                        ? getInspectionDeadline(m.inspectionFrequency, m.lastSuccessfulRun.endTime!)
                        : undefined,
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
                    <InspectionSection />
                </Tabs.Panel>
                <Tabs.Panel>
                    <StyledView>
                        <StyledContent>
                            <StyledButtons>
                                <ScheduleMissionDialog />
                                <MissionButton />
                            </StyledButtons>
                            {allMissions && <AllInspectionsTable inspections={allMissions} />}
                        </StyledContent>
                    </StyledView>
                </Tabs.Panel>
            </Tabs.Panels>
        </Tabs>
    )
}
