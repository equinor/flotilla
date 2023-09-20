import { Typography, Tabs } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { RefreshProps } from '../FrontPage/FrontPage'
import { MissionDefinition } from 'models/MissionDefinition'
import { InspectionSection } from './InspectionSection'
import { useEffect, useState } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'

export function InspectionOverviewSection({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const [activeTab, setActiveTab] = useState(0)
    const [selectedMissions, setSelectedMissions] = useState<MissionDefinition[]>()

    const handleChange = (index: number) => {
        setActiveTab(index)
    }

    useEffect(() => {
        const foo = async () => {
            let response = await BackendAPICaller.getMissionDefinitions({ pageSize: 100 }).then(
                (response) => response.content
            )
            // setSelectedMissions(response)
            console.log(response)
        }

        foo()
    }, [activeTab])

    return (
        <Tabs activeTab={activeTab} onChange={handleChange}>
            <Tabs.List>
                <Tabs.Tab>{TranslateText('Deck Overview')}</Tabs.Tab>
                <Tabs.Tab>{TranslateText('Predefined Missions')}</Tabs.Tab>
            </Tabs.List>
            <Tabs.Panels>
                <Tabs.Panel>
                    <InspectionSection refreshInterval={refreshInterval} />
                </Tabs.Panel>
                <Tabs.Panel>
                    <InspectionSection refreshInterval={refreshInterval} />
                </Tabs.Panel>
            </Tabs.Panels>
        </Tabs>
    )
}
