import { DefaultPage } from 'components/Pages/DefaultPage/DefaultPage'
import { MissionControlSection } from '../Pages/FrontPage/MissionOverview/MissionControlSection'
import { InspectionSection } from 'components/Pages/InspectionPage/InspectionSection'
import { InspectionOverviewSection } from 'components/Pages/InspectionPage/InspectionOverview'
import { MissionHistoryView } from 'components/Pages/MissionHistory/MissionHistoryView'
import { AutoScheduleSection } from 'components/Pages/FrontPage/AutoScheduleSection/AutoScheduleSection'

export const MissionControlPage = () => {
    return (
        <DefaultPage pageName="missionControl">
            <MissionControlSection />
        </DefaultPage>
    )
}

export const DeckOverviewPage = () => {
    return (
        <DefaultPage pageName="inspectionOverview'">
            <InspectionSection />
        </DefaultPage>
    )
}

export const PredefinedMissionsPage = () => {
    return (
        <DefaultPage pageName="predefinedMissions">
            <InspectionOverviewSection />
        </DefaultPage>
    )
}

export const MissionHistoryPage = () => {
    return (
        <DefaultPage pageName="history">
            <MissionHistoryView refreshInterval={1000} />
        </DefaultPage>
    )
}

export const AutoSchedulePage = () => {
    return (
        <DefaultPage pageName="autoSchedule">
            <AutoScheduleSection />
        </DefaultPage>
    )
}
