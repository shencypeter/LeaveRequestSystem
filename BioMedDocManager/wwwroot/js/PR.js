
var page_name = "";

//$('.navbar').load('navbar.html');
//$('.banner-menu').load('menu.html');
//$('.banner-footer').load('footer.html');

function PeoplePurchaseTables()
{
    $('.banner').load('PeoplePurchaseTables.html');
    page_name = '請購';
    
}

  function scrollMenu(distance) {
    const menu = document.getElementById('scrollableMenu');
    menu.scrollBy({ left: distance, behavior: 'smooth' });
  }