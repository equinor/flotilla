import {
    Autocomplete,
    AutocompleteChanges,
    Button,
    Card,
    Dialog,
    Icon,
    Search,
    TextField,
    Typography,
} from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionStatus } from 'models/Mission'
import { ChangeEvent, useState } from 'react'
import { Icons } from 'utils/icons'
import { InspectionType } from 'models/Inspection'
import { useMissionFilterContext } from 'components/Contexts/MissionFilterContext'

const StyledHeader = styled.div`
    display: grid;
    grid-template-columns: auto 100px 150px;
    gap: 1rem;
`

const StyledDialog = styled(Card)`
    display: flex;
    padding: 1rem;
    width: 600px;
    right: 175px;
`

export function FilterSection() {
    const { TranslateText } = useLanguageContext()
    const [isFilteringDialogOpen, setIsFilteringDialogOpen] = useState<boolean>(false)
    const [formattedMinStartTime, setFormattedMinStartTime] = useState<string | undefined>(undefined)
    const [formattedMaxStartTime, setFormattedMaxStartTime] = useState<string | undefined>(undefined)
    const [formattedMinEndTime, setFormattedMinEndTime] = useState<string | undefined>(undefined)
    const [formattedMaxEndTime, setFormattedMaxEndTime] = useState<string | undefined>(undefined)
    const { filterFunctions, filterState } = useMissionFilterContext()

    const missionStatusTranslationMap: Map<string, MissionStatus> = new Map(
        Object.values(MissionStatus).map((missionStatus) => {
            return [TranslateText(missionStatus), missionStatus]
        })
    )

    const inspectionTypeTranslationMap: Map<string, InspectionType> = new Map(
        Object.values(InspectionType).map((inspectionType) => {
            return [TranslateText(inspectionType), inspectionType]
        })
    )

    const changeMinStartTime = (newMinStartTime: string | undefined) => {
        filterFunctions.switchMinStartTime(newMinStartTime ? new Date(newMinStartTime).getTime() / 1000 : undefined)
        setFormattedMinStartTime(newMinStartTime)
    }

    const changeMaxStartTime = (newMaxStartTime: string | undefined) => {
        filterFunctions.switchMaxStartTime(newMaxStartTime ? new Date(newMaxStartTime).getTime() / 1000 : undefined)
        setFormattedMaxStartTime(newMaxStartTime)
    }

    const changeMinEndTime = (newMinEndTime: string | undefined) => {
        filterFunctions.switchMinEndTime(newMinEndTime ? new Date(newMinEndTime).getTime() / 1000 : undefined)
        setFormattedMinEndTime(newMinEndTime)
    }

    const changeMaxEndTime = (newMaxEndTime: string | undefined) => {
        filterFunctions.switchMaxEndTime(newMaxEndTime ? new Date(newMaxEndTime).getTime() / 1000 : undefined)
        setFormattedMaxEndTime(newMaxEndTime)
    }

    const onClickFilterIcon = () => {
        setIsFilteringDialogOpen(true)
    }

    const onFilterClose = () => {
        setIsFilteringDialogOpen(false)
    }

    const onClearFilters = () => {
        filterFunctions.resetFilters()
    }

    return (
        <>
            <StyledHeader>
                <Search
                    value={filterState.missionName ?? ''}
                    placeholder={TranslateText('Search for missions')}
                    onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                        filterFunctions.switchMissionName(changes.target.value)
                    }}
                />
                <Button onClick={onClickFilterIcon}>
                    <Icon name={Icons.Filter} size={32} />
                    {TranslateText('Filter')}
                </Button>
                <Button variant="outlined" onClick={onClearFilters}>
                    <Icon name={Icons.Clear} size={32} />
                    {TranslateText('Clear all filters')}
                </Button>
            </StyledHeader>
            <Dialog open={isFilteringDialogOpen} isDismissable>
                <StyledDialog>
                    <StyledHeader>
                        <Typography variant="h2">{TranslateText('Filter')}</Typography>
                        <Button variant="ghost_icon" onClick={onFilterClose}>
                            <Icon name={Icons.Clear} size={32} />
                        </Button>
                    </StyledHeader>
                    <Autocomplete
                        options={Array.from(missionStatusTranslationMap.keys())}
                        onOptionsChange={(changes: AutocompleteChanges<string>) => {
                            filterFunctions.switchStatuses(
                                changes.selectedItems.map((selectedItem) => {
                                    return missionStatusTranslationMap.get(selectedItem)!
                                })
                            )
                        }}
                        placeholder={`${filterState.statuses.length}/${
                            Array.from(missionStatusTranslationMap.keys()).length
                        } ${TranslateText('selected')}`}
                        label={TranslateText('Mission status')}
                        initialSelectedOptions={filterState.statuses.map((status) => {
                            return TranslateText(status)
                        })}
                        multiple
                        autoWidth={true}
                        onFocus={(e) => e.preventDefault()}
                    />
                    <Autocomplete
                        options={Array.from(inspectionTypeTranslationMap.keys())}
                        onOptionsChange={(changes: AutocompleteChanges<string>) => {
                            filterFunctions.switchInspectionTypes(
                                changes.selectedItems.map((selectedItem) => {
                                    return inspectionTypeTranslationMap.get(selectedItem)!
                                })
                            )
                        }}
                        placeholder={`${filterState.inspectionTypes.length}/${
                            Array.from(inspectionTypeTranslationMap.keys()).length
                        } ${TranslateText('selected')}`}
                        label={TranslateText('Inspection type')}
                        initialSelectedOptions={filterState.inspectionTypes.map((inspectionType) => {
                            return TranslateText(inspectionType)
                        })}
                        multiple
                        autoWidth={true}
                        onFocus={(e) => e.preventDefault()}
                    />
                    <Search
                        value={filterState.robotName ?? ''}
                        placeholder={TranslateText('Search for a robot name')}
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            filterFunctions.switchRobotName(changes.target.value)
                        }}
                    />
                    <Search
                        value={filterState.tagId ?? ''}
                        placeholder={TranslateText('Search for a tag')}
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            filterFunctions.switchTagId(changes.target.value)
                        }}
                    />
                    <TextField
                        id="datetime"
                        value={formattedMinStartTime}
                        label={TranslateText('Select min start time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            changeMinStartTime(changes.target.value)
                        }}
                    />
                    <TextField
                        id="datetime"
                        value={formattedMaxStartTime}
                        label={TranslateText('Select max start time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            changeMaxStartTime(changes.target.value)
                        }}
                    />
                    <TextField
                        id="datetime"
                        value={formattedMinEndTime}
                        label={TranslateText('Select min end time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            changeMinEndTime(changes.target.value)
                        }}
                    />
                    <TextField
                        id="datetime"
                        value={formattedMaxEndTime}
                        label={TranslateText('Select max end time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            changeMaxEndTime(changes.target.value)
                        }}
                    />
                </StyledDialog>
            </Dialog>
        </>
    )
}
