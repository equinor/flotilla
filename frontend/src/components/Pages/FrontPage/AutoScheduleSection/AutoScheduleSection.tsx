import { Autocomplete, Button, Icon, Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { StyledDialog, StyledTableBody, StyledTableCell, TextAlignedButton } from 'components/Styles/StyledComponents'
import { allDays, allDaysIndexOfToday, DaysOfWeek } from 'models/AutoScheduleFrequency'
import styled from 'styled-components'
import { capitalizeFirstLetter } from 'utils/StringFormatting'
import { Icons } from 'utils/icons'
import { memo, useState } from 'react'
import { FormCard } from 'components/Pages/MissionDefinitionPage/MissionDefinitionStyledComponents'
import { MissionDefinitionEditDialogContent } from 'components/Pages/MissionDefinitionPage/MissionDefinitionPage'
import { MissionDefinition } from 'models/MissionDefinition'
import { AutoScheduleMissionTableRow } from './AutoScheduleMissionTableRow'
import { CalendarPro } from './AutoScheduleCalendar'

const StyledSection = styled.div`
    display: flex;
    flex-direction: column;
    max-width: 960px;
    gap: 1rem;

    @media (min-width: 600px) {
        min-width: 90vw;
    }
`
const StyledDayOverview = styled.div`
    display: grid;
    gap: 0px;
`
const StyledView = styled.div`
    display: flex;
    align-items: flex-start;
`
const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    gap: 30px;
    align-items: end;
    @media (max-width: 600px) {
        align-items: start;
    }
    max-width: 95%;
`

const StyledDesktopView = styled.div`
    @media (max-width: 600px) {
        display: none;
    }
`

const StyledPhoneView = styled.div`
    @media (min-width: 600px) {
        display: none;
    }
`

const StyledButtons = styled.div`
    display: flex;
    flex-direction: row;
    gap: 10px;
`

const SelectMissionsComponent = memo(
    ({
        missions,
        selectedMissions,
        setSelectedMissions,
        multiple = true,
    }: {
        missions: MissionDefinition[]
        selectedMissions: MissionDefinition[]
        setSelectedMissions: (missions: MissionDefinition[]) => void
        multiple?: boolean
    }) => {
        const { TranslateText } = useLanguageContext()

        return (
            <Autocomplete
                dropdownHeight={200}
                optionLabel={(m) => m.name}
                options={missions}
                onOptionsChange={(changes) => setSelectedMissions(changes.selectedItems)}
                label={TranslateText('Select missions')}
                multiple={multiple}
                selectedOptions={selectedMissions}
                placeholder={`${selectedMissions.length}/${missions.length} ${TranslateText('selected')}`}
                autoWidth
                onFocus={(e) => e.preventDefault()}
            />
        )
    }
)

const EditAutoSchedulingButton = () => {
    const { TranslateText } = useLanguageContext()
    const { missionDefinitions } = useMissionDefinitionsContext()

    const [dialogOpen, setDialogOpen] = useState<boolean>(false)
    const [selectedMissions, setSelectedMissions] = useState<MissionDefinition[]>([])

    const openDialog = () => {
        setDialogOpen(true)
    }
    const closeDialog = () => {
        setDialogOpen(false)
        setSelectedMissions([])
    }

    const UpdateAutoSchedulingDialogContent = () => (
        <>
            <StyledDialog.Header>
                <StyledDialog.Title>
                    <Typography variant="h3">
                        {TranslateText('Edit auto scheduling of mission') + ' ' + selectedMissions[0]?.name}
                    </Typography>
                </StyledDialog.Title>
            </StyledDialog.Header>
            <FormCard>
                <MissionDefinitionEditDialogContent
                    missionDefinition={selectedMissions[0]}
                    fieldName="autoScheduleFrequency"
                    closeEditDialog={closeDialog}
                />
            </FormCard>
        </>
    )

    const SelectMissionDialogContent = () => (
        <>
            <StyledDialog.Header>
                <StyledDialog.Title>
                    <Typography variant="h3">{TranslateText('Select mission for auto scheduling')}</Typography>
                </StyledDialog.Title>
            </StyledDialog.Header>
            <StyledDialog.CustomContent>
                <SelectMissionsComponent
                    missions={missionDefinitions}
                    selectedMissions={selectedMissions}
                    setSelectedMissions={setSelectedMissions}
                    multiple={false}
                />
            </StyledDialog.CustomContent>
            <StyledDialog.Actions>
                <Button onClick={closeDialog} variant="outlined" color="primary">
                    {TranslateText('Cancel')}
                </Button>
            </StyledDialog.Actions>
        </>
    )
    return (
        <>
            <TextAlignedButton onClick={openDialog}>
                <Icon name={Icons.Add} size={24} />
                {TranslateText('Edit auto scheduling')}
            </TextAlignedButton>
            <StyledDialog open={dialogOpen}>
                {selectedMissions.length === 1 ? <UpdateAutoSchedulingDialogContent /> : <SelectMissionDialogContent />}
            </StyledDialog>
        </>
    )
}

const DayTable = ({ day, isToday }: { day: DaysOfWeek; isToday: boolean }) => {
    const { TranslateText } = useLanguageContext()
    const { missionDefinitions } = useMissionDefinitionsContext()

    const autoScheduledMissionDefinitionsForDay = missionDefinitions.filter((m) =>
        m.autoScheduleFrequency?.daysOfWeek.includes(day)
    )
    const timeMissionPairs = autoScheduledMissionDefinitionsForDay
        .map((mission) =>
            mission.autoScheduleFrequency!.timesOfDayCET.map((time) => {
                return { time, mission }
            })
        )
        .flat()
        .sort((a, b) => (a.time === b.time ? 0 : a.time > b.time ? 1 : -1))

    return (
        <Table key={day}>
            <Table.Head>
                <Table.Row>
                    <StyledTableCell>
                        {capitalizeFirstLetter(TranslateText(day))}
                        {isToday && ` (${TranslateText('today')})`}
                    </StyledTableCell>
                </Table.Row>
            </Table.Head>
            <StyledTableBody>
                {timeMissionPairs.length > 0 ? (
                    timeMissionPairs.map(({ time, mission }) => (
                        <AutoScheduleMissionTableRow key={time + mission.id} day={day} time={time} mission={mission} />
                    ))
                ) : (
                    <Table.Row>
                        <Table.Cell>
                            <Typography>{TranslateText('No missions')}</Typography>
                        </Table.Cell>
                    </Table.Row>
                )}
            </StyledTableBody>
        </Table>
    )
}

const DayOverview = () => {
    const { TranslateText } = useLanguageContext()
    const { missionDefinitions } = useMissionDefinitionsContext()

    const autoScheduleMissionDefinitions = missionDefinitions.filter((m) => m.autoScheduleFrequency)
    const allDaysSortedByToday = allDays.slice(allDaysIndexOfToday).concat(allDays.slice(0, allDaysIndexOfToday))

    return (
        <StyledSection>
            {autoScheduleMissionDefinitions.length > 0 ? (
                <>
                    <Typography>
                        {TranslateText('These missions will be automatically scheduled at the specified time')}
                    </Typography>
                    <StyledDayOverview>
                        {allDaysSortedByToday.map((day, index) => (
                            <DayTable key={day} day={day} isToday={index === 0} />
                        ))}
                    </StyledDayOverview>
                </>
            ) : (
                <Typography>{TranslateText('There are currently no automatically scheduled missions.')}</Typography>
            )}
        </StyledSection>
    )
}

export const AutoScheduleSection = () => {
    const [showListView, setShowListView] = useState(false)
    const { TranslateText } = useLanguageContext()

    return (
        <StyledView>
            <StyledContent>
                <StyledButtons>
                    <Button variant="ghost" onClick={() => setShowListView(!showListView)}>
                        <Icon name={showListView ? Icons.ViewWeek : Icons.List} size={24} />
                        {showListView ? TranslateText('Switch to calendar view') : TranslateText('Switch to list view')}
                    </Button>
                    <EditAutoSchedulingButton />
                </StyledButtons>
                <StyledDesktopView>{showListView ? <DayOverview /> : <CalendarPro />}</StyledDesktopView>
                <StyledPhoneView>
                    <DayOverview />
                </StyledPhoneView>
            </StyledContent>
        </StyledView>
    )
}
