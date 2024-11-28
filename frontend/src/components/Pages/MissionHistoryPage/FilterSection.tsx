import {
    Autocomplete,
    AutocompleteChanges,
    Button,
    Dialog,
    Icon,
    Search,
    TextField,
    Typography,
} from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionStatusFilterOptions, missionStatusFilterOptionsIterable } from 'models/Mission'
import { ChangeEvent, useState } from 'react'
import { Icons } from 'utils/icons'
import { InspectionType } from 'models/Inspection'
import { useMissionFilterContext } from 'components/Contexts/MissionFilterContext'
import { tokens } from '@equinor/eds-tokens'

const StyledHeader = styled.div`
    display: flex;
    align-items: center;
    gap: 1rem;
    @media (max-width: 730px) {
        max-width: 300px;
    }
`
const StyledSearch = styled(Search)`
    display: flex;
    width: 288px;
    height: 36px;
    align-items: center;
    gap: 8px;
    border-bottom: none;
    border: 1px solid ${tokens.colors.ui.background__medium.hex};
    --eds-input-background: white;
`
const StyledButtonDiv = styled.div`
    display: flex;
    gap: 1rem;
`
const StyledDialog = styled(Dialog)`
    width: calc(100vw * 0.9);
    max-width: 700px;
`
const StyledDialogContent = styled.div`
    display: flex;
    flex-direction: column;
    padding: 20px;
    gap: 1rem;
`

export const FilterSection = () => {
    const { TranslateText } = useLanguageContext()
    const { filterFunctions, filterState } = useMissionFilterContext()
    const [isFilteringDialogOpen, setIsFilteringDialogOpen] = useState<boolean>(false)

    const missionStatusTranslationMap: Map<string, MissionStatusFilterOptions> = new Map(
        Object.values(missionStatusFilterOptionsIterable).map((missionStatus) => {
            return [TranslateText(missionStatus), missionStatus]
        })
    )

    const inspectionTypeTranslationMap: Map<string, InspectionType> = new Map(
        Object.values(InspectionType).map((inspectionType) => {
            return [TranslateText(inspectionType), inspectionType]
        })
    )

    const onClickFilterIcon = () => {
        setIsFilteringDialogOpen(true)
    }

    const onFilterClose = () => {
        setIsFilteringDialogOpen(false)
    }

    const onClearFilters = () => {
        filterFunctions.removeFilters()
    }

    return (
        <>
            <StyledHeader>
                <StyledSearch
                    value={filterState.missionName ?? ''}
                    placeholder={TranslateText('Search for missions')}
                    onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                        filterFunctions.switchMissionName(changes.target.value)
                    }}
                />
                <StyledButtonDiv>
                    <Button onClick={onClickFilterIcon} variant="ghost">
                        <Icon name={Icons.Filter} size={24} />
                        {TranslateText('Filter')}
                    </Button>
                    <Button variant="ghost" onClick={onClearFilters}>
                        <Icon name={Icons.Clear} size={24} />
                        {TranslateText('Clear all filters')}
                    </Button>
                </StyledButtonDiv>
            </StyledHeader>
            <StyledDialog open={isFilteringDialogOpen} isDismissable>
                <StyledDialogContent>
                    <Dialog.Header>
                        <Typography variant="h2">{TranslateText('Filter')}</Typography>
                        <Button variant="ghost_icon" onClick={onFilterClose}>
                            <Icon name={Icons.Clear} size={32} />
                        </Button>
                    </Dialog.Header>
                    <Autocomplete
                        options={Array.from(missionStatusTranslationMap.keys())}
                        onOptionsChange={(changes: AutocompleteChanges<string>) => {
                            filterFunctions.switchStatuses(
                                changes.selectedItems.map((selectedItem) => {
                                    return missionStatusTranslationMap.get(selectedItem)!
                                })
                            )
                        }}
                        placeholder={`${filterState.statuses ? filterState.statuses.length : 0}/${
                            Array.from(missionStatusTranslationMap.keys()).length
                        } ${TranslateText('selected')}`}
                        label={TranslateText('Mission status')}
                        initialSelectedOptions={
                            filterState.statuses
                                ? filterState.statuses.map((status) => {
                                      return TranslateText(status)
                                  })
                                : []
                        }
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
                        placeholder={`${filterState.inspectionTypes ? filterState.inspectionTypes.length : 0}/${
                            Array.from(inspectionTypeTranslationMap.keys()).length
                        } ${TranslateText('selected')}`}
                        label={TranslateText('Inspection type')}
                        initialSelectedOptions={
                            filterState.inspectionTypes
                                ? filterState.inspectionTypes.map((inspectionType) => {
                                      return TranslateText(inspectionType)
                                  })
                                : []
                        }
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
                        value={filterFunctions.dateTimeIntToString(filterState.minStartTime) ?? ''}
                        label={TranslateText('Select min start time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            filterFunctions.switchMinStartTime(
                                filterFunctions.dateTimeStringToInt(changes.target.value)
                            )
                        }}
                    />
                    <TextField
                        id="datetime"
                        value={filterFunctions.dateTimeIntToString(filterState.maxStartTime) ?? ''}
                        label={TranslateText('Select max start time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            filterFunctions.switchMaxStartTime(
                                filterFunctions.dateTimeStringToInt(changes.target.value)
                            )
                        }}
                    />
                    <TextField
                        id="datetime"
                        value={filterFunctions.dateTimeIntToString(filterState.minEndTime) ?? ''}
                        label={TranslateText('Select min end time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            filterFunctions.switchMinEndTime(filterFunctions.dateTimeStringToInt(changes.target.value))
                        }}
                    />
                    <TextField
                        id="datetime"
                        value={filterFunctions.dateTimeIntToString(filterState.maxEndTime) ?? ''}
                        label={TranslateText('Select max end time')}
                        type="datetime-local"
                        onChange={(changes: ChangeEvent<HTMLInputElement>) => {
                            filterFunctions.switchMaxEndTime(filterFunctions.dateTimeStringToInt(changes.target.value))
                        }}
                    />
                </StyledDialogContent>
            </StyledDialog>
        </>
    )
}
