select group_concat(width) as widthes, *
from (
	select *
	from stats
	where httpstatus=200
	and UserAgent != "-"
	group by Image, Width
)
group by Image
order by count(width) desc