import { Button, Icon, Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { StyledDialog, StyledTableBody, StyledTableCell, TextAlignedButton } from 'components/Styles/StyledComponents'
import { allDays, allDaysIndexOfToday, DaysOfWeek } from 'models/AutoScheduleFrequency'
import styled from 'styled-components'
import { capitalizeFirstLetter } from 'utils/StringFormatting'
import { Icons } from 'utils/icons'
import { useState } from 'react'
import { FormCard } from 'components/Pages/MissionDefinitionPage/MissionDefinitionStyledComponents'
import { MissionDefinitionEditDialogContent } from 'components/Pages/MissionDefinitionPage/MissionDefinitionPage'
import { MissionDefinition } from 'models/MissionDefinition'
import { SelectMissionsComponent } from '../MissionOverview/ScheduleMissionDialog/SelectMissionsToScheduleDialog'
import { AutoScheduleMissionTableRow } from './AutoScheduleMissionTableRow'

const StyledSection = styled.div`
    display: flex;
    flex-direction: column;
    max-width: 960px;
    gap: 1rem;
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
    max-width: 960px;
`

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
        .sort((a, b) => (a.time > b.time ? 1 : -1))

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
    return (
        <StyledView>
            <StyledContent>
                <EditAutoSchedulingButton />
                <DayOverview />
            </StyledContent>
        </StyledView>
    )
}
